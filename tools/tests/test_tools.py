"""
tools/ のスクリプトの単体テスト（音の合成・アイコン・ビルド検査）。
  python -m unittest discover -s tools/tests -v
"""
import gzip
import importlib.util
import io
import os
import shutil
import tempfile
import unittest
from contextlib import redirect_stdout

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.abspath(os.path.join(HERE, ".."))
ROOT = os.path.abspath(os.path.join(TOOLS, ".."))


def load(name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(TOOLS, name + ".py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


audio = load("make_audio")
icons = load("make_icons")
check = load("check_build")


class AudioTests(unittest.TestCase):
    def test_midi_a4_is_440(self):
        self.assertAlmostEqual(audio.midi(69), 440.0)
        self.assertAlmostEqual(audio.midi(81), 880.0)

    def test_shaped_noise_is_normalized_and_loops(self):
        x = audio.shaped_noise(44100, 1.0, 100, 2000)
        self.assertEqual(len(x), 44100)
        self.assertAlmostEqual(float(np.max(np.abs(x))), 1.0, places=5)
        self.assertLess(abs(x[0] - x[-1]), 0.25)

    def test_every_sfx_is_short_finite_and_not_silent(self):
        for name, fn in audio.SFX.items():
            s = fn()
            self.assertTrue(np.all(np.isfinite(s)), name)
            self.assertGreater(float(np.max(np.abs(s))), 1e-3, name)
            self.assertLess(len(s) / audio.SR, 2.0, f"{name} は 2 秒より短い")
            self.assertGreater(len(s) / audio.SR, 0.05, name)

    def test_ambience_loops_seamlessly(self):
        a = audio.ambience(4.0)
        self.assertEqual(a.shape, (4 * audio.SR, 2))
        # 先頭と末尾がつながる（ループ長の整数倍の周期だけ使っている）
        self.assertLess(float(np.max(np.abs(a[0] - a[-1]))), 0.08)
        self.assertLess(float(np.max(np.abs(a))), 1.5)

    def test_write_makes_a_stereo_16bit_wav(self):
        tmp = tempfile.mkdtemp()
        try:
            old = audio.OUT
            audio.OUT = tmp
            with redirect_stdout(io.StringIO()):
                audio.write("x.wav", audio.sfx_pop())
            audio.OUT = old
            import wave
            with wave.open(os.path.join(tmp, "x.wav"), "rb") as w:
                self.assertEqual(w.getnchannels(), 2)
                self.assertEqual(w.getsampwidth(), 2)
                self.assertEqual(w.getframerate(), audio.SR)
        finally:
            shutil.rmtree(tmp)


class IconTests(unittest.TestCase):
    def test_app_icon_is_opaque_and_foreground_is_transparent_outside(self):
        im = icons.app_icon().resize((64, 64))
        self.assertEqual(im.getpixel((32, 32))[3], 255)
        fg = icons.app_icon_fg().resize((64, 64))
        self.assertEqual(fg.getpixel((2, 2))[3], 0, "アダプティブアイコンの前景は外側が透明")
        self.assertGreater(fg.getpixel((24, 32))[3], 0, "まん中に目がある")

    def test_small_icons_have_content(self):
        for fn in (icons.icon_book, icons.icon_gear, icons.icon_close, icons.icon_eye, lambda: icons.icon_sound(True), lambda: icons.icon_sound(False)):
            im = fn().resize((64, 64))
            alpha = np.array(im)[:, :, 3]
            self.assertGreater(int((alpha > 128).sum()), 100)
            self.assertLess(int((alpha > 128).sum()), 64 * 64 * 0.9)


class CheckBuildTests(unittest.TestCase):
    def make_site(self, with_jslib=True, gz=False):
        tmp = tempfile.mkdtemp()
        os.makedirs(os.path.join(tmp, "Build"))
        with open(os.path.join(tmp, "index.html"), "w", encoding="utf-8") as f:
            f.write('<script>var x = buildUrl + "/Gyorome.loader.js";</script><link rel="manifest" href="manifest.webmanifest">')
        with open(os.path.join(tmp, "manifest.webmanifest"), "w") as f:
            f.write("{}")
        for n in ("Gyorome.loader.js", "Gyorome.data", "Gyorome.wasm"):
            with open(os.path.join(tmp, "Build", n), "wb") as f:
                f.write(b"x")
        code = b"function GyoRegisterPageEvents(){}" if with_jslib else b"nothing"
        fw = os.path.join(tmp, "Build", "Gyorome.framework.js" + (".gz" if gz else ""))
        with open(fw, "wb") as f:
            f.write(gzip.compress(code) if gz else code)
        return tmp

    def run_check(self, site):
        old = check.sys.argv
        check.sys.argv = ["check_build.py", site]
        try:
            with redirect_stdout(io.StringIO()) as out:
                check.main()
            return out.getvalue()
        finally:
            check.sys.argv = old

    def test_ok_site_passes(self):
        site = self.make_site()
        try:
            self.assertIn("OK", self.run_check(site))
        finally:
            shutil.rmtree(site)

    def test_gzipped_framework_is_inspected(self):
        site = self.make_site(gz=True)
        try:
            self.assertIn("OK", self.run_check(site))
        finally:
            shutil.rmtree(site)

    def test_missing_jslib_fails(self):
        site = self.make_site(with_jslib=False)
        try:
            with self.assertRaises(SystemExit):
                self.run_check(site)
        finally:
            shutil.rmtree(site)

    def test_do_not_ship_folder_fails(self):
        site = self.make_site()
        try:
            os.makedirs(os.path.join(site, "Gyorome_BackUpThisFolder_ButDontShipItWithYourGame"))
            with self.assertRaises(SystemExit):
                self.run_check(site)
        finally:
            shutil.rmtree(site)


class RepoTests(unittest.TestCase):
    def test_workflows_exist_and_reference_the_build_methods(self):
        wf = os.path.join(ROOT, ".github", "workflows")
        pages = open(os.path.join(wf, "deploy-pages.yml"), encoding="utf-8").read()
        self.assertIn("Gyorome.EditorTools.BuildScript.BuildWebGL", pages)
        self.assertIn("game-ci/unity-builder", pages)
        self.assertIn("upload-pages-artifact", pages)
        rel = open(os.path.join(wf, "release.yml"), encoding="utf-8").read()
        for t in ("Android", "iOS", "StandaloneWindows64", "StandaloneOSX", "StandaloneLinux64"):
            self.assertIn(t, rel)
        self.assertIn("Gyorome.EditorTools.BuildScript.Build", rel)

    def test_steam_templates_mention_every_depot(self):
        vdf = open(os.path.join(TOOLS, "steam", "app_build.vdf"), encoding="utf-8").read()
        self.assertIn('"AppID"', vdf)
        for d in ("depot_windows.vdf", "depot_macos.vdf", "depot_linux.vdf"):
            self.assertIn(d, vdf)
            self.assertTrue(os.path.exists(os.path.join(TOOLS, "steam", d)), d)


if __name__ == "__main__":
    unittest.main()
