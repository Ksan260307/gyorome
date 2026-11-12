"""
ぎょろめ — 目玉アセット生成スクリプト（Blender 5.x）

目玉を部品ごとにプロシージャルに作り、Unity 用の FBX と編集用の .blend を書き出します。
  Eyeball  : 白目（半径 1 の球）。頂点カラーはほぼ白、うしろ側だけ少し赤みがかる
  Iris     : 虹彩（球面のふた）。UV = (中心からの距離 0..1, 角度 0..1)、頂点カラー = 模様の明るさ
             ひとみ（瞳孔）は Unity のシェーダーが UV から描く（大きさ・形を自由に変えられるように）
  LidUpper : 上まぶた（厚みのある球の殻。ひらいた姿勢で作る。Unity では目の中心のまわりに回して閉じる）
  LidLower : 下まぶた
  Tear     : なみだ・あせ のしずく

座標（Blender）: 目が見ている向き = -Y、上 = +Z。FBX（-Z forward / Y up）にすると Unity では +Z を向く。
色は頂点カラー Col（sRGB）。まぶたと白目は Unity 側でマテリアルの色をかけるので、ほぼ白と灰色で作る。

使い方:
  blender -b --factory-startup --python blender/scripts/build_eye_kit.py -- [fbx_out_dir] [blend_out]
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

TAU = math.tau

# まぶたの形（度）。Unity 側（EyeRig）と同じ値にしておく
UPPER_CAP_DEG = 100.0        # 上まぶた：ふたの角半径
UPPER_POLE_OPEN_DEG = 155.0  # ひらいたときの、ふたの中心の仰角（前 = 0、上 = 90、うしろ = 180）
LOWER_CAP_DEG = 70.0
LOWER_POLE_OPEN_DEG = -125.0
IRIS_CAP_DEG = 26.0          # 虹彩の角半径
EYE_R = 1.0

VC_MATERIAL = None


# ---------------------------------------------------------------------------
# 引数
# ---------------------------------------------------------------------------
def parse_args():
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    here = os.path.dirname(os.path.abspath(__file__))
    repo = os.path.abspath(os.path.join(here, "..", ".."))
    out = a[0] if len(a) > 0 else os.path.join(repo, "unity", "Assets", "Art", "Models")
    blend = a[1] if len(a) > 1 else os.path.join(repo, "blender", "eye_kit.blend")
    return out, blend


# ---------------------------------------------------------------------------
# 色・数学
# ---------------------------------------------------------------------------
def hexc(h, a=1.0):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, a)


def clamp01(x):
    return 0.0 if x < 0.0 else (1.0 if x > 1.0 else x)


def mixc(c1, c2, t):
    t = clamp01(t)
    return tuple(c1[i] + (c2[i] - c1[i]) * t for i in range(4))


def sstep(e0, e1, x):
    if e0 == e1:
        return 0.0 if x < e0 else 1.0
    t = clamp01((x - e0) / (e1 - e0))
    return t * t * (3.0 - 2.0 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def s2l(c):
    def f(x):
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4
    return (f(c[0]), f(c[1]), f(c[2]), c[3])


def elev_dir(deg):
    """仰角（度）→ 向き。前 = -Y、上 = +Z"""
    r = math.radians(deg)
    return Vector((0.0, -math.cos(r), math.sin(r)))


# ---------------------------------------------------------------------------
# メッシュビルダー
# ---------------------------------------------------------------------------
class MB:
    """頂点・頂点カラー・UV・面を蓄積する簡易メッシュビルダー"""

    def __init__(self):
        self.V = []
        self.C = []
        self.UV = []
        self.F = []

    def v(self, p, c, uv=(0.0, 0.0)):
        self.V.append(Vector(p))
        self.C.append(tuple(c))
        self.UV.append(tuple(uv))
        return len(self.V) - 1

    def f(self, *ids):
        # 極の扇（同じ頂点が 2 回ならぶ四角）は三角にする
        out = []
        for i in ids:
            if not out or out[-1] != i:
                out.append(i)
        if len(out) > 1 and out[0] == out[-1]:
            out.pop()
        if len(set(out)) < 3:
            return
        self.F.append(tuple(out))


def connect_rings(mb, rings, closed=True, flip=False):
    """同じ数の頂点を持つ輪どうしを四角でつなぐ（輪が 1 点なら三角で扇）"""
    for a, b in zip(rings[:-1], rings[1:]):
        n = max(len(a), len(b))
        for j in range(n if closed else n - 1):
            k = (j + 1) % n
            a0 = a[j % len(a)]
            a1 = a[k % len(a)]
            b0 = b[j % len(b)]
            b1 = b[k % len(b)]
            ids = (a0, b0, b1, a1)
            if flip:
                ids = tuple(reversed(ids))
            mb.f(*ids)


def vc_material():
    global VC_MATERIAL
    if VC_MATERIAL is not None:
        return VC_MATERIAL
    mat = bpy.data.materials.new("VertexColor")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = None
    for nd in nt.nodes:
        if nd.type == "BSDF_PRINCIPLED":
            bsdf = nd
    try:
        attr = nt.nodes.new("ShaderNodeVertexColor")
        attr.layer_name = "Col"
        if bsdf is not None:
            nt.links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
            if "Roughness" in bsdf.inputs:
                bsdf.inputs["Roughness"].default_value = 0.5
    except Exception as e:  # noqa
        print("material setup warning:", e)
    VC_MATERIAL = mat
    return mat


def build(mb, name, smooth=True):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(p) for p in mb.V], [], mb.F)
    me.validate(clean_customdata=False)
    me.update(calc_edges=True)
    attr = me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    vidx = [0] * len(me.loops)
    me.loops.foreach_get("vertex_index", vidx)
    lin = [s2l(c) for c in mb.C]
    flat = []
    for vi in vidx:
        flat.extend(lin[vi] if vi < len(lin) else (1, 1, 1, 1))
    attr.data.foreach_set("color", flat)
    try:
        me.color_attributes.active_color = attr
        me.color_attributes.render_color_index = me.color_attributes.active_color_index
    except Exception:
        pass
    uv = me.uv_layers.new(name="UVMap")
    flat_uv = []
    for vi in vidx:
        flat_uv.extend(mb.UV[vi] if vi < len(mb.UV) else (0.0, 0.0))
    uv.uv.foreach_set("vector", flat_uv)
    me.polygons.foreach_set("use_smooth", [smooth] * len(me.polygons))
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(vc_material())
    return ob


def frame_to(pole):
    """ローカル +Z を pole の向きにする回転行列"""
    z = Vector(pole).normalized()
    ref = Vector((0.0, -1.0, 0.0))
    if abs(z.dot(ref)) > 0.999:
        ref = Vector((0.0, 0.0, 1.0))
    x = ref.cross(z).normalized()
    y = z.cross(x).normalized()
    return Matrix((
        (x.x, y.x, z.x, 0.0),
        (x.y, y.y, z.y, 0.0),
        (x.z, y.z, z.z, 0.0),
        (0.0, 0.0, 0.0, 1.0),
    ))


def cap_rings(mb, radius, cap_deg, pole, colfn, seg=64, rings=20, uvfn=None, start_deg=0.0):
    """pole を中心にした球面のふた（角半径 cap_deg）の輪のリストを作る。colfn(t_deg, a, p)"""
    m = frame_to(pole)
    out = []
    for i in range(rings + 1):
        t = lerp(start_deg, cap_deg, i / rings)
        if i == 0 and start_deg == 0.0:
            p = m @ Vector((0.0, 0.0, radius))
            out.append([mb.v(p, colfn(0.0, 0.0, p), uvfn(0.0, 0.0) if uvfn else (0.0, 0.0))])
            continue
        tr = math.radians(t)
        ring = []
        for j in range(seg):
            a = TAU * j / seg
            local = Vector((math.sin(tr) * math.cos(a), math.sin(tr) * math.sin(a), math.cos(tr))) * radius
            p = m @ local
            ring.append(mb.v(p, colfn(t, a, p), uvfn(t / cap_deg, a / TAU) if uvfn else (0.0, 0.0)))
        out.append(ring)
    return out


# ---------------------------------------------------------------------------
# 部品
# ---------------------------------------------------------------------------
SCLERA = hexc("#fbf9f6")
SCLERA_BACK = hexc("#e8b9b4")
LID = hexc("#ffffff")
LID_EDGE = hexc("#9c9c9c")
LASH = hexc("#3a3a3a")
TEAR_TOP = hexc("#dff3ff")
TEAR_BOTTOM = hexc("#9fd4ff")


def make_eyeball(seg=64, rings=40):
    """白目：半径 1 の球。前 = -Y。うしろ側（見えにくい所）は少し赤みがかる"""
    mb = MB()
    forward = Vector((0.0, -1.0, 0.0))

    def col(t, a, p):
        back = sstep(0.55, 1.0, -p.normalized().dot(forward))
        return mixc(SCLERA, SCLERA_BACK, back * 0.6)

    rl = cap_rings(mb, EYE_R, 180.0, forward, col, seg=seg, rings=rings)
    # 最後の輪（うしろの極）は 1 点にまとめる
    pole = mb.v((0.0, EYE_R, 0.0), col(180.0, 0.0, Vector((0.0, EYE_R, 0.0))))
    connect_rings(mb, rl[:-1])
    connect_rings(mb, [rl[-2], [pole]])
    return build(mb, "Eyeball")


def iris_pattern(r, a):
    """虹彩の模様（0..1）：放射状のすじ + 外側の暗い輪 + ひとみのまわりの明るさ"""
    rays = 0.5 + 0.5 * math.sin(a * 24.0 + 1.3 * math.sin(a * 7.0 + r * 9.0))
    rays2 = 0.5 + 0.5 * math.sin(a * 11.0 - r * 14.0)
    crypt = sstep(0.25, 0.55, r) * (1.0 - sstep(0.75, 0.95, r))
    v = 0.62 + 0.26 * (rays * 0.7 + rays2 * 0.3) * (0.4 + 0.6 * crypt)
    v += 0.12 * (1.0 - sstep(0.0, 0.3, r))
    v -= 0.55 * sstep(0.84, 1.0, r)
    return clamp01(v)


def make_iris(seg=96, rings=18):
    """虹彩：前（-Y）を中心にした球面のふた。少し外側（r = 1.004）に浮かせる。UV = (中心からの距離, 角度)"""
    mb = MB()
    forward = Vector((0.0, -1.0, 0.0))

    def col(t, a, p):
        v = iris_pattern(t / IRIS_CAP_DEG, a)
        return (v, v, v, 1.0)

    def uv(r, a):
        return (r, a)

    rl = cap_rings(mb, EYE_R * 1.004, IRIS_CAP_DEG, forward, col, seg=seg, rings=rings, uvfn=uv)
    connect_rings(mb, rl)
    return build(mb, "Iris")


def make_lid(name, cap_deg, pole_deg, r_out, r_in, seg=72, rings=28, lash=0.07):
    """まぶた：厚みのある球の殻（外面・内面・ふち）。ふちの近くは少し暗く（まつ毛の線）"""
    mb = MB()
    pole = elev_dir(pole_deg)

    def col_out(t, a, p):
        edge = sstep(cap_deg * (1.0 - lash), cap_deg, t)
        return mixc(LID, LASH, edge * 0.85)

    def col_in(t, a, p):
        return mixc(LID_EDGE, LASH, 0.5)

    outer = cap_rings(mb, r_out, cap_deg, pole, col_out, seg=seg, rings=rings)
    connect_rings(mb, outer)
    inner = cap_rings(mb, r_in, cap_deg, pole, col_in, seg=seg, rings=max(4, rings // 3))
    connect_rings(mb, inner, flip=True)
    # ふち：外面の最後の輪と内面の最後の輪を、少し外にふくらんだ中間の輪でつなぐ
    mid_r = (r_out + r_in) * 0.5
    mid_deg = cap_deg + math.degrees((r_out - r_in) * 0.5 / mid_r)
    mid = cap_rings(mb, mid_r, mid_deg, pole, lambda t, a, p: LASH, seg=seg, rings=1, start_deg=mid_deg)[-1]
    connect_rings(mb, [outer[-1], mid])
    connect_rings(mb, [mid, inner[-1]])
    return build(mb, name)


def make_lid_upper():
    return make_lid("LidUpper", UPPER_CAP_DEG, UPPER_POLE_OPEN_DEG, 1.09, 1.04)


def make_lid_lower():
    return make_lid("LidLower", LOWER_CAP_DEG, LOWER_POLE_OPEN_DEG, 1.06, 1.02, lash=0.09)


def make_tear(seg=24, rings=12):
    """なみだ・あせ：小さなしずく（先が上）。高さ約 2 で作り Unity で縮める"""
    mb = MB()
    rl = []
    for i in range(rings + 1):
        t = i / rings
        th = math.pi * t
        r = math.sin(th) * (1.0 - 0.45 * (1.0 - t)) if i not in (0, rings) else 0.0
        z = math.cos(th) * (1.0 + 0.35 * (1.0 - t))
        c = mixc(TEAR_TOP, TEAR_BOTTOM, t)
        if r < 1e-6:
            rl.append([mb.v((0.0, 0.0, z), c)])
            continue
        ring = []
        for j in range(seg):
            a = TAU * j / seg
            ring.append(mb.v((r * math.cos(a), r * math.sin(a), z), c))
        rl.append(ring)
    connect_rings(mb, rl)
    return build(mb, "Tear")


# ---------------------------------------------------------------------------
# 書き出し
# ---------------------------------------------------------------------------
def export_fbx(ob, out_dir):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    path = os.path.join(out_dir, ob.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="OFF",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        colors_type="SRGB",
        prioritize_active_color=True,
        add_leaf_bones=False,
        use_tspace=False,
        embed_textures=False,
        path_mode="AUTO",
    )
    return path


def make_all():
    return [make_eyeball(), make_iris(), make_lid_upper(), make_lid_lower(), make_tear()]


def main():
    out_dir, blend_out = parse_args()
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(os.path.dirname(blend_out), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    objs = make_all()
    for ob in objs:
        p = export_fbx(ob, out_dir)
        print("exported", ob.name, len(ob.data.vertices), "verts ->", os.path.basename(p))
    # .blend では、組み立てた目（原点）のとなりに、しずくを置く
    for ob in objs:
        if ob.name == "Tear":
            ob.location = (3.0, 0.0, 0.0)
    bpy.ops.wm.save_as_mainfile(filepath=blend_out, compress=True)
    print("saved", blend_out)


if __name__ == "__main__":
    main()
