"""
ぎょろめの音をすべてプログラムで合成する（numpy のみ）。
  python tools/make_audio.py
出力: unity/Assets/Audio/*.wav
 - ambience.wav : やわらかいパッドのループ（つなぎ目なし・小さな音）
 - 効果音: まばたき・つつく（ぼいん）・ぷにっ・ぽん・くすくす・めまい・いびき・あくび・びっくり・
   うなる・きらきら・ひゅっ・ちりん・ぽちゃん・くしゃみ・ごろごろ・がたがた・コンコン・ぐるん・
   ぼよよん・笑い・音符・ジャックポット・ぷしゅー・ちゅっ・ぴょん
"""
import os
import sys
import wave

import numpy as np

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "unity", "Assets", "Audio")
rng = np.random.default_rng(20261111)


def write(name, data, peak_level=0.85):
    data = np.asarray(data, dtype=np.float64)
    if data.ndim == 1:
        data = np.stack([data, data], axis=1)
    peak = np.max(np.abs(data)) + 1e-9
    data = data / peak * peak_level
    pcm = (np.clip(data, -1, 1) * 32767).astype(np.int16)
    path = os.path.join(OUT, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print("wrote", name, f"{len(pcm) / SR:.2f}s")


def t_axis(sec):
    return np.arange(int(sec * SR)) / SR


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def env_adsr(n, a, d, s, r):
    e = np.ones(n) * s
    ai = max(int(a * SR), 1)
    di = int(d * SR)
    ri = int(r * SR)
    e[:ai] = np.linspace(0, 1, ai)
    e[ai:ai + di] = np.linspace(1, s, len(e[ai:ai + di]))
    if ri > 0:
        e[-ri:] *= np.linspace(1, 0, ri)
    return e


def env_decay(n, k):
    return np.exp(-np.linspace(0, k, n))


def shaped_noise(n, color=1.0, lo=20.0, hi=20000.0):
    """周波数領域で色付けしたノイズ（FFT 合成なのでそのままループする）"""
    spec = rng.normal(size=n // 2 + 1) + 1j * rng.normal(size=n // 2 + 1)
    f = np.fft.rfftfreq(n, 1 / SR)
    f[0] = 1.0
    mag = 1.0 / f ** (color / 2)
    mag *= 1 / (1 + (f / hi) ** 4)
    mag *= 1 / (1 + (lo / f) ** 4)
    x = np.fft.irfft(spec * mag, n)
    return x / (np.max(np.abs(x)) + 1e-9)


def glide(t, f0, f1, curve=1.0):
    """f0 から f1 へ周波数がすべる正弦波（位相を積分）"""
    u = (t / t[-1]) ** curve if len(t) > 1 else t
    f = f0 + (f1 - f0) * u
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph)


def lowpass(x, cutoff):
    """簡単な 1 次ローパス"""
    rc = 1.0 / (2 * np.pi * cutoff)
    a = (1.0 / SR) / (rc + 1.0 / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def bell(freq, dur=0.8, bright=1.0):
    t = t_axis(dur)
    partials = [(1.0, 1.0, 3.0), (2.76, 0.35 * bright, 5.0), (5.4, 0.18 * bright, 8.0), (8.93, 0.08 * bright, 12.0)]
    s = np.zeros_like(t)
    for ratio, amp, dk in partials:
        s += amp * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-dk * t)
    return s * env_adsr(len(t), 0.002, 0.05, 0.9, 0.05)


def pluck(freq, dur=0.5):
    t = t_axis(dur)
    s = np.sin(2 * np.pi * freq * t) + 0.25 * np.sin(2 * np.pi * freq * 2 * t) * np.exp(-8 * t)
    return s * np.exp(-6 * t) * env_adsr(len(t), 0.002, 0.02, 1.0, 0.03)


def stereo(x, pan=0.0):
    l = np.cos((pan + 1) * np.pi / 4)
    r = np.sin((pan + 1) * np.pi / 4)
    return np.stack([x * l, x * r], axis=1)


# ---------------------------------------------------------------------------
# 効果音
# ---------------------------------------------------------------------------
def sfx_blink():
    t = t_axis(0.09)
    n = shaped_noise(len(t), 1.5, 300, 2500)
    return n * env_adsr(len(t), 0.004, 0.03, 0.3, 0.04)


def sfx_poke():
    t = t_axis(0.28)
    s = glide(t, 520, 180, 0.6) + 0.3 * glide(t, 1040, 360, 0.6)
    return s * env_adsr(len(t), 0.003, 0.08, 0.5, 0.15)


def sfx_squish():
    t = t_axis(0.32)
    n = shaped_noise(len(t), 2.0, 120, 1200)
    s = n * 0.7 + 0.5 * glide(t, 260, 90, 0.8)
    return s * env_adsr(len(t), 0.01, 0.1, 0.5, 0.18)


def sfx_pop():
    t = t_axis(0.09)
    return glide(t, 900, 400, 0.5) * env_decay(len(t), 9)


def sfx_giggle(notes=(72, 76, 79, 76), step=0.09):
    out = np.zeros(int((len(notes) * step + 0.3) * SR))
    for i, n in enumerate(notes):
        t = t_axis(0.16)
        s = np.sin(2 * np.pi * midi(n) * t) * (1 + 0.15 * np.sin(2 * np.pi * 12 * t))
        s += 0.3 * np.sin(2 * np.pi * midi(n) * 2 * t)
        s *= env_adsr(len(t), 0.005, 0.04, 0.5, 0.08)
        a = int(i * step * SR)
        out[a:a + len(s)] += s
    return out


def sfx_dizzy():
    t = t_axis(1.1)
    f = 440 * 2 ** (-t * 0.8) * (1 + 0.06 * np.sin(2 * np.pi * 7 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    s = np.sin(ph) + 0.3 * np.sin(2 * ph)
    return s * env_adsr(len(t), 0.02, 0.2, 0.6, 0.4)


def sfx_snore():
    t = t_axis(1.6)
    n = shaped_noise(len(t), 2.5, 60, 500)
    breath = 0.5 + 0.5 * np.sin(2 * np.pi * 0.62 * t - np.pi / 2)
    rattle = 1 + 0.5 * np.sign(np.sin(2 * np.pi * 28 * t))
    s = n * breath ** 2 * rattle
    return s * env_adsr(len(t), 0.1, 0.1, 1.0, 0.2)


def sfx_yawn():
    t = t_axis(0.95)
    f = 220 + 140 * np.sin(np.pi * t / t[-1])
    ph = 2 * np.pi * np.cumsum(f) / SR
    s = np.sin(ph) + 0.5 * np.sin(2 * ph) * (0.3 + 0.7 * (t / t[-1])) + 0.2 * np.sin(3 * ph)
    s = lowpass(s, 1200)
    return s * env_adsr(len(t), 0.15, 0.1, 0.9, 0.3)


def sfx_surprise():
    t = t_axis(0.3)
    s = glide(t, 600, 1300, 0.7) + 0.4 * glide(t, 1200, 2600, 0.7)
    return s * env_adsr(len(t), 0.005, 0.05, 0.7, 0.12)


def sfx_growl():
    t = t_axis(0.55)
    f = 95 * (1 + 0.08 * np.sin(2 * np.pi * 9 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    saw = 2 * ((ph / (2 * np.pi)) % 1.0) - 1
    s = lowpass(saw, 700) + 0.35 * shaped_noise(len(t), 1.5, 80, 900)
    return s * env_adsr(len(t), 0.03, 0.1, 0.8, 0.2)


def sfx_sparkle():
    notes = [88, 92, 95, 100, 104]
    out = np.zeros(int(1.0 * SR))
    for i, n in enumerate(notes):
        s = bell(midi(n), 0.6, 0.6)
        a = int(i * 0.07 * SR)
        out[a:a + len(s)] += s * 0.7
    return out


def sfx_whoosh():
    t = t_axis(0.36)
    n = shaped_noise(len(t), 1.0, 300, 6000)
    sweep = np.sin(np.pi * t / t[-1]) ** 1.5
    return n * sweep


def sfx_chime():
    return bell(midi(84), 0.9, 1.0) + 0.6 * bell(midi(91), 0.9, 0.8)


def sfx_drip():
    t = t_axis(0.22)
    s = glide(t, 500, 1500, 0.4) * env_decay(len(t), 10)
    return s


def sfx_sneeze():
    t = t_axis(0.5)
    n = shaped_noise(len(t), 0.8, 400, 8000)
    burst = env_adsr(len(t), 0.004, 0.08, 0.2, 0.3)
    s = n * burst + 0.5 * glide(t, 700, 300, 0.4) * env_adsr(len(t), 0.03, 0.1, 0.3, 0.3)
    return s


def sfx_purr():
    t = t_axis(1.3)
    f = 42 * (1 + 0.03 * np.sin(2 * np.pi * 0.7 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    am = 0.55 + 0.45 * np.sign(np.sin(ph))
    s = lowpass(shaped_noise(len(t), 1.5, 60, 1500), 900) * am
    return s * env_adsr(len(t), 0.15, 0.1, 1.0, 0.25)


def sfx_rattle():
    t = t_axis(0.55)
    out = np.zeros_like(t)
    for _ in range(14):
        a = int(rng.uniform(0, 0.45) * SR)
        c = t_axis(0.03)
        click = shaped_noise(len(c), 0.5, 800, 9000) * env_decay(len(c), 12)
        out[a:a + len(click)] += click * rng.uniform(0.5, 1.0)
    return out


def sfx_knock():
    out = np.zeros(int(0.35 * SR))
    for i in range(2):
        t = t_axis(0.12)
        s = glide(t, 320, 180, 0.3) * env_decay(len(t), 25) + 0.3 * shaped_noise(len(t), 1.0, 200, 3000) * env_decay(len(t), 40)
        a = int(i * 0.16 * SR)
        out[a:a + len(s)] += s
    return out


def sfx_spin():
    t = t_axis(0.7)
    f = 300 * 2 ** (t * 1.6) * (1 + 0.04 * np.sin(2 * np.pi * 18 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * env_adsr(len(t), 0.02, 0.1, 0.8, 0.25)


def sfx_bounce():
    out = np.zeros(int(0.9 * SR))
    for i, (f0, gain) in enumerate([(420, 1.0), (380, 0.7), (340, 0.45)]):
        t = t_axis(0.22)
        s = glide(t, f0, f0 * 0.4, 0.6) * env_adsr(len(t), 0.003, 0.06, 0.5, 0.1) * gain
        a = int(i * 0.26 * SR)
        out[a:a + len(s)] += s
    return out


def sfx_laugh():
    return sfx_giggle(notes=(67, 71, 74, 71, 67, 71, 74, 78), step=0.085)


def sfx_note():
    return pluck(midi(76), 0.45)


def sfx_jackpot():
    notes = [72, 76, 79, 84, 79, 84, 88]
    out = np.zeros(int(1.6 * SR))
    for i, n in enumerate(notes):
        s = bell(midi(n), 0.9, 1.0)
        a = int(i * 0.11 * SR)
        out[a:a + len(s)] += s
    return out


def sfx_hiss():
    t = t_axis(0.5)
    n = shaped_noise(len(t), 0.7, 1500, 9000)
    return n * env_adsr(len(t), 0.01, 0.1, 0.6, 0.35)


def sfx_kiss():
    t = t_axis(0.18)
    s = glide(t, 1800, 500, 0.3) * env_decay(len(t), 14) + 0.4 * shaped_noise(len(t), 1.0, 500, 5000) * env_decay(len(t), 20)
    return s


def sfx_hop():
    t = t_axis(0.2)
    return glide(t, 380, 760, 0.5) * env_adsr(len(t), 0.004, 0.05, 0.6, 0.1)


def sfx_hiccup():
    t = t_axis(0.16)
    return glide(t, 500, 900, 0.3) * env_adsr(len(t), 0.004, 0.03, 0.3, 0.08) + 0.3 * glide(t, 250, 450, 0.3) * env_decay(len(t), 20)


# ---------------------------------------------------------------------------
# 環境音（ループ）
# ---------------------------------------------------------------------------
def ambience(sec=12.0):
    n = int(sec * SR)
    t = np.arange(n) / SR
    out = np.zeros((n, 2))
    # ゆっくりゆれるパッド（F - A - C - E）。ループ長の整数倍の周期だけ使うので、つなぎ目がない
    for i, note in enumerate([53, 57, 60, 64]):
        f = midi(note)
        cycles = round(f * sec)
        f = cycles / sec
        lfo = 0.5 + 0.5 * np.sin(2 * np.pi * (1.0 / sec) * t * (i + 1) + i)
        s = np.sin(2 * np.pi * f * t) * (0.35 + 0.65 * lfo) + 0.15 * np.sin(2 * np.pi * f * 2 * t) * lfo
        pan = (i - 1.5) * 0.4
        out += stereo(s * 0.25, pan)
    # とても小さな空気の音
    air = shaped_noise(n, 2.2, 80, 1200)
    out += stereo(air * 0.08)
    return out


SFX = {
    "blink": sfx_blink,
    "poke": sfx_poke,
    "squish": sfx_squish,
    "pop": sfx_pop,
    "giggle": sfx_giggle,
    "dizzy": sfx_dizzy,
    "snore": sfx_snore,
    "yawn": sfx_yawn,
    "surprise": sfx_surprise,
    "growl": sfx_growl,
    "sparkle": sfx_sparkle,
    "whoosh": sfx_whoosh,
    "chime": sfx_chime,
    "drip": sfx_drip,
    "sneeze": sfx_sneeze,
    "purr": sfx_purr,
    "rattle": sfx_rattle,
    "knock": sfx_knock,
    "spin": sfx_spin,
    "bounce": sfx_bounce,
    "laugh": sfx_laugh,
    "note": sfx_note,
    "jackpot": sfx_jackpot,
    "hiss": sfx_hiss,
    "kiss": sfx_kiss,
    "hop": sfx_hop,
    "hiccup": sfx_hiccup,
}


def main(only=None):
    os.makedirs(OUT, exist_ok=True)
    for name, fn in SFX.items():
        if only and name not in only:
            continue
        write(name + ".wav", fn())
    if not only or "ambience" in only:
        write("ambience.wav", ambience(), peak_level=0.5)


if __name__ == "__main__":
    main(set(sys.argv[1:]) or None)
