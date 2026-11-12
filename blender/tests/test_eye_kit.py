"""
目玉アセット生成スクリプトのテスト（Blender の中で実行する）。
  blender -b --factory-startup --python-exit-code 1 --python blender/tests/test_eye_kit.py
"""
import math
import os
import sys
import unittest

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "scripts"))
import build_eye_kit as kit  # noqa: E402


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.VC_MATERIAL = None


def radii(ob):
    return [v.co.length for v in ob.data.vertices]


def centroid(ob):
    s = Vector((0, 0, 0))
    for v in ob.data.vertices:
        s += v.co
    return s / max(1, len(ob.data.vertices))


class EyeKitTests(unittest.TestCase):
    def setUp(self):
        reset()

    def test_eyeball_is_unit_sphere_facing_minus_y(self):
        ob = kit.make_eyeball()
        rs = radii(ob)
        self.assertAlmostEqual(min(rs), 1.0, delta=0.01)
        self.assertAlmostEqual(max(rs), 1.0, delta=0.01)
        self.assertGreater(len(ob.data.polygons), 1000)
        self.assertIsNotNone(ob.data.color_attributes.get("Col"))
        col = ob.data.color_attributes["Col"]
        front = [l.index for l in ob.data.loops if ob.data.vertices[l.vertex_index].co.y < -0.95]
        back = [l.index for l in ob.data.loops if ob.data.vertices[l.vertex_index].co.y > 0.95]
        self.assertTrue(front and back)
        fc = col.data[front[0]].color
        bc = col.data[back[0]].color
        self.assertGreater(fc[1], 0.9, "前は白")
        self.assertLess(bc[1], fc[1], "うしろは赤みがかる（緑が少ない）")

    def test_iris_is_a_cap_on_the_front(self):
        ob = kit.make_iris()
        c = centroid(ob)
        self.assertLess(c.y, -0.85, "虹彩は前（-Y）にある")
        self.assertAlmostEqual(c.x, 0.0, delta=0.01)
        self.assertAlmostEqual(c.z, 0.0, delta=0.01)
        rs = radii(ob)
        self.assertGreater(min(rs), 1.0, "白目の少し外側に浮かせる")
        self.assertLess(max(rs), 1.01)
        max_ang = max(math.degrees(math.acos(max(-1.0, min(1.0, -v.co.normalized().y)))) for v in ob.data.vertices)
        self.assertAlmostEqual(max_ang, kit.IRIS_CAP_DEG, delta=0.5)
        uv = ob.data.uv_layers["UVMap"].uv
        us = [uv[i].vector.x for i in range(len(uv))]
        vs = [uv[i].vector.y for i in range(len(uv))]
        self.assertAlmostEqual(min(us), 0.0, delta=1e-4)
        self.assertAlmostEqual(max(us), 1.0, delta=1e-4)
        self.assertGreaterEqual(min(vs), 0.0)
        self.assertLessEqual(max(vs), 1.0)

    def test_iris_pattern_is_dark_at_the_rim_and_bright_near_the_center(self):
        self.assertLess(kit.iris_pattern(0.98, 0.3), kit.iris_pattern(0.1, 0.3))
        for r in (0.0, 0.3, 0.6, 0.9, 1.0):
            for a in (0.0, 1.0, 2.0, 4.0):
                v = kit.iris_pattern(r, a)
                self.assertGreaterEqual(v, 0.0)
                self.assertLessEqual(v, 1.0)

    def test_lids_sit_outside_the_eyeball_and_cover_the_right_angles(self):
        up = kit.make_lid_upper()
        lo = kit.make_lid_lower()
        self.assertGreater(min(radii(up)), 1.03)
        self.assertLess(max(radii(up)), 1.10)
        self.assertGreater(min(radii(lo)), 1.01)
        self.assertLess(max(radii(lo)), 1.07)
        self.assertGreater(centroid(up).z, 0.2, "上まぶたは上寄り")
        self.assertLess(centroid(lo).z, -0.2, "下まぶたは下寄り")
        # ひらいた姿勢：上まぶたの前のふちは仰角 55 度あたり（ひとみは見える）
        front_edge = min(math.degrees(math.atan2(v.co.z, -v.co.y)) for v in up.data.vertices if abs(v.co.x) < 0.05 and v.co.y < 0)
        self.assertAlmostEqual(front_edge, kit.UPPER_POLE_OPEN_DEG - kit.UPPER_CAP_DEG, delta=2.0)
        lower_edge = max(math.degrees(math.atan2(v.co.z, -v.co.y)) for v in lo.data.vertices if abs(v.co.x) < 0.05 and v.co.y < 0)
        self.assertAlmostEqual(lower_edge, kit.LOWER_POLE_OPEN_DEG + kit.LOWER_CAP_DEG, delta=2.0)
        rs = radii(up)
        self.assertGreater(max(rs) - min(rs), 0.04, "厚みがある")

    def test_tear_is_a_small_drop_pointing_up(self):
        ob = kit.make_tear()
        zs = [v.co.z for v in ob.data.vertices]
        self.assertLess(max(zs) - min(zs), 3.0)
        top = [v for v in ob.data.vertices if v.co.z > max(zs) - 0.01]
        self.assertLess(max(math.hypot(v.co.x, v.co.y) for v in top), 0.05, "先がとがる")
        mid = [v for v in ob.data.vertices if abs(v.co.z) < 0.2]
        self.assertGreater(max(math.hypot(v.co.x, v.co.y) for v in mid), 0.5, "まん中はふくらむ")

    def test_all_parts_have_vertex_colors_and_uvs(self):
        for ob in kit.make_all():
            self.assertIsNotNone(ob.data.color_attributes.get("Col"), ob.name)
            self.assertIsNotNone(ob.data.uv_layers.get("UVMap"), ob.name)
            self.assertGreater(len(ob.data.polygons), 100, ob.name)


if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(EyeKitTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        sys.exit(1)
