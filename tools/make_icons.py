"""
アイコンを生成する（PIL）。4 倍で描いて縮小し、なめらかにする。
  python tools/make_icons.py
出力:
  unity/Assets/UI/Icons/app_icon.png      1024px  アプリのアイコン（ストア用・全プラットフォーム）
  unity/Assets/UI/Icons/app_icon_fg.png   1024px  Android アダプティブアイコンの前景（目玉だけ・透明背景）
  unity/Assets/UI/Icons/app_icon_bg.png   1024px  Android アダプティブアイコンの背景（単色）
  unity/Assets/UI/Icons/icon_*.png        64px    UI のボタン（本・歯車・とじる・音・ふるえ・ヒント）
  unity/Assets/WebGLTemplates/Gyorome/icon.png / favicon.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, ".."))
OUT = os.path.join(ROOT, "unity", "Assets", "UI", "Icons")
WEB = os.path.join(ROOT, "unity", "Assets", "WebGLTemplates", "Gyorome")
S = 4

BG = (255, 214, 112, 255)        # たまご色の背景
SCLERA = (252, 250, 246, 255)
PUPIL = (24, 22, 30, 255)
LINE = (70, 60, 50, 255)


def canvas(size):
    return Image.new("RGBA", (size * S, size * S), (0, 0, 0, 0))


def save(img, path, size):
    img = img.resize((size, size), Image.LANCZOS)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote", os.path.relpath(path, ROOT))


def draw_eye(d, cx, cy, r, look=(0.0, 0.0), lid=0.0, outline=True):
    """ひとつの目：白目・虹彩・ひとみ・ハイライト・（上まぶた）"""
    if outline:
        d.ellipse([cx - r * 1.06, cy - r * 1.06, cx + r * 1.06, cy + r * 1.06], fill=LINE)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=SCLERA)
    ix = cx + look[0] * r * 0.42
    iy = cy + look[1] * r * 0.42
    ir = r * 0.48
    # 黒目：模様のない黒い丸
    d.ellipse([ix - ir, iy - ir, ix + ir, iy + ir], fill=PUPIL)
    hr = r * 0.17
    d.ellipse([ix - ir * 0.45 - hr, iy - ir * 0.5 - hr, ix - ir * 0.45 + hr, iy - ir * 0.5 + hr], fill=(255, 255, 255, 255))
    hr2 = r * 0.07
    d.ellipse([ix + ir * 0.35 - hr2, iy + ir * 0.35 - hr2, ix + ir * 0.35 + hr2, iy + ir * 0.35 + hr2], fill=(255, 255, 255, 230))
    if lid > 0:
        # 上まぶた：目の上をおおう弧
        top = cy - r * 1.06
        edge = cy - r + 2 * r * lid
        d.chord([cx - r * 1.08, top, cx + r * 1.08, top + (edge - top) * 2], 180, 360, fill=BG)
        d.line([cx - r * 1.0, edge, cx + r * 1.0, edge], fill=LINE, width=int(r * 0.09))


def app_icon(transparent=False):
    n = 1024
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    if not transparent:
        d.rounded_rectangle([0, 0, w, w], radius=w * 0.22, fill=BG)
    # 2 つの目（少し内側を見る）
    r = w * 0.2
    draw_eye(d, w * 0.31, w * 0.5, r, look=(0.25, 0.1))
    draw_eye(d, w * 0.69, w * 0.5, r, look=(-0.25, 0.1))
    return im


def app_icon_bg():
    n = 1024
    im = canvas(n)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, n * S, n * S], fill=BG)
    return im


def app_icon_fg():
    # アダプティブアイコンは外側 1/6 が切り取られるので、中央 66% に描く
    n = 1024
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    r = w * 0.14
    draw_eye(d, w * 0.365, w * 0.5, r, look=(0.25, 0.1))
    draw_eye(d, w * 0.635, w * 0.5, r, look=(-0.25, 0.1))
    return im


def icon_book():
    n = 64
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    col = (255, 255, 255, 255)
    d.rounded_rectangle([w * 0.16, w * 0.14, w * 0.84, w * 0.86], radius=w * 0.08, outline=col, width=int(w * 0.07))
    d.line([w * 0.5, w * 0.14, w * 0.5, w * 0.86], fill=col, width=int(w * 0.06))
    for y in (0.34, 0.5, 0.66):
        d.line([w * 0.25, w * y, w * 0.42, w * y], fill=col, width=int(w * 0.05))
        d.line([w * 0.58, w * y, w * 0.75, w * y], fill=col, width=int(w * 0.05))
    return im


def icon_gear():
    n = 64
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    c = w / 2
    col = (255, 255, 255, 255)
    pts = []
    for i in range(16):
        a = i / 16 * math.tau
        rr = w * (0.42 if i % 2 == 0 else 0.33)
        pts.append((c + math.cos(a) * rr, c + math.sin(a) * rr))
        a2 = (i + 0.5) / 16 * math.tau
        pts.append((c + math.cos(a2) * rr, c + math.sin(a2) * rr))
    d.polygon(pts, fill=col)
    d.ellipse([c - w * 0.14, c - w * 0.14, c + w * 0.14, c + w * 0.14], fill=(0, 0, 0, 0))
    return im


def icon_close():
    n = 64
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    col = (255, 255, 255, 255)
    d.line([w * 0.24, w * 0.24, w * 0.76, w * 0.76], fill=col, width=int(w * 0.1))
    d.line([w * 0.76, w * 0.24, w * 0.24, w * 0.76], fill=col, width=int(w * 0.1))
    return im


def icon_sound(on=True):
    n = 64
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    col = (255, 255, 255, 255)
    d.polygon([(w * 0.18, w * 0.4), (w * 0.34, w * 0.4), (w * 0.52, w * 0.24), (w * 0.52, w * 0.76), (w * 0.34, w * 0.6), (w * 0.18, w * 0.6)], fill=col)
    if on:
        for k, rr in enumerate((0.18, 0.3)):
            d.arc([w * 0.5 - w * rr, w * 0.5 - w * rr, w * 0.5 + w * rr, w * 0.5 + w * rr], -40, 40, fill=col, width=int(w * 0.06))
    else:
        d.line([w * 0.6, w * 0.38, w * 0.82, w * 0.62], fill=col, width=int(w * 0.07))
        d.line([w * 0.82, w * 0.38, w * 0.6, w * 0.62], fill=col, width=int(w * 0.07))
    return im


def icon_eye():
    n = 64
    im = canvas(n)
    d = ImageDraw.Draw(im)
    w = n * S
    col = (255, 255, 255, 255)
    d.ellipse([w * 0.1, w * 0.26, w * 0.9, w * 0.74], outline=col, width=int(w * 0.07))
    d.ellipse([w * 0.36, w * 0.36, w * 0.64, w * 0.64], fill=col)
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    save(app_icon(), os.path.join(OUT, "app_icon.png"), 1024)
    save(app_icon_fg(), os.path.join(OUT, "app_icon_fg.png"), 1024)
    save(app_icon_bg(), os.path.join(OUT, "app_icon_bg.png"), 1024)
    save(icon_book(), os.path.join(OUT, "icon_book.png"), 64)
    save(icon_gear(), os.path.join(OUT, "icon_gear.png"), 64)
    save(icon_close(), os.path.join(OUT, "icon_close.png"), 64)
    save(icon_sound(True), os.path.join(OUT, "icon_sound_on.png"), 64)
    save(icon_sound(False), os.path.join(OUT, "icon_sound_off.png"), 64)
    save(icon_eye(), os.path.join(OUT, "icon_eye.png"), 64)
    save(app_icon(), os.path.join(WEB, "icon.png"), 256)
    save(app_icon(), os.path.join(WEB, "favicon.png"), 64)


if __name__ == "__main__":
    main()
