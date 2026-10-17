"""Builds sound variants by layering real recordings according to a JSON recipe (see recipes/*.json).

  py -I layer_build.py <recipe.json> [out_dir]

Ranges are [lo, hi]; equal ends mean a fixed value (no random draw). Variant v uses seed recipe.seed + v, so the
same recipe and variant number always give the same file.
"""
import glob, json, os, sys
import numpy as np, soundfile as sf
from scipy.signal import butter, fftconvolve, resample_poly, sosfilt

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from analyze import BANDS, bands, load, onsets, target  # noqa: E402

SR = 44100
SOURCES = 'D:/DaD_Sounds/_sources'


def trim(x, db=-40):
    return x[np.argmax(np.abs(x) > np.abs(x).max() * 10 ** (db / 20)):]


def norm(x):
    return x / (np.abs(x).max() + 1e-9)


def hp(x, f):
    return sosfilt(butter(2, f, 'hp', fs=SR, output='sos'), x) if f else x


def lp(x, f):
    return sosfilt(butter(2, f, 'lp', fs=SR, output='sos'), x) if f else x


def shelf_hi(x, f, g):
    return x + (10 ** (g / 20) - 1) * hp(x, f)


def pitch(x, semis):
    r = 2 ** (semis / 12)
    return resample_poly(x, 1000, int(round(1000 * r))) if r != 1 else x


def snap(x, hold_ms, tau_ms):
    t = np.arange(len(x)) / SR * 1000
    return x * np.where(t < hold_ms, 1, np.exp(-(t - hold_ms) / tau_ms))


def room(x, rng, db, decay_ms=70, len_ms=350, band=(400, 6000)):
    n = int(SR * len_ms / 1000)
    ir = rng.standard_normal(n) * np.exp(-np.arange(n) / (SR * decay_ms / 1000))
    ir = sosfilt(butter(2, band, 'bp', fs=SR, output='sos'), ir)
    w = fftconvolve(x, ir)[:len(x)]
    return x + w / (np.abs(w).max() + 1e-9) * np.abs(x).max() * 10 ** (db / 20)


def match(x, tgt, k):
    g = np.clip((np.asarray(tgt) - bands(x, SR)) * k, -10, 10)
    centers = np.array([np.sqrt(a * b) for a, b in BANDS])
    X = np.fft.rfft(x)
    f = np.maximum(np.fft.rfftfreq(len(x), 1 / SR), 20)
    X *= 10 ** (np.interp(np.log(f), np.log(centers), g) / 20)
    return np.fft.irfft(X, len(x))


def fade(x, ms):
    n = min(len(x), int(SR * ms / 1000))
    x = x.copy()
    x[-n:] *= np.linspace(1, 0, n) ** 2
    return x


def clips(root, pattern):
    out = []
    for f in sorted(glob.glob(os.path.join(root, pattern))):
        x, sr = load(f)
        out.append(norm(trim(resample_poly(x, SR, sr) if sr != SR else x)))
    if not out:
        sys.exit(f'no source files match {pattern} under {root}')
    return out


def build(recipe, out):
    root = recipe.get('sources', SOURCES)
    layers = [(L, clips(root, L['src'])) for L in recipe['layers']]
    tgt = recipe.get('target') or (target(recipe['reference']).tolist() if recipe.get('reference') else None)
    os.makedirs(out, exist_ok=True)
    for v in range(recipe.get('variants', 12)):
        rng = np.random.default_rng(recipe.get('seed', 1000) + v)
        draw = lambda r: r[0] if r[0] == r[1] else rng.uniform(*r)
        mix = np.zeros(int(SR * recipe['length_ms'] / 1000))

        def put(sig, t_ms, g_db):
            i = int(SR * t_ms / 1000)
            if i < len(mix):
                s = sig[:len(mix) - i] * 10 ** (g_db / 20)
                mix[i:i + len(s)] += s

        def hit(L, pool, t=None):
            s = pool[rng.integers(len(pool))]
            s = lp(hp(pitch(s, draw(L.get('pitch', [0, 0]))), L.get('hp')), L.get('lp'))
            if 'snap' in L:
                s = snap(s, draw(L['snap']['hold']), draw(L['snap']['tau']))
            if 'max_ms' in L:
                s = fade(s[:int(SR * L['max_ms'] / 1000)], min(30, L['max_ms'] / 3))
            put(s, draw(L['at_ms']) if t is None else t, draw(L['gain_db']))

        for L, pool in layers:
            if 'chance' in L and rng.uniform() >= L['chance']:
                continue
            if 'count' not in L:
                hit(L, pool)
                continue
            t = draw(L['at_ms'])
            for _ in range(rng.integers(L['count'][0], L['count'][1] + 1)):
                hit(L, pool, t)
                t += draw(L['gap_ms'])
                if t > L.get('until_ms', recipe['length_ms']):
                    break
        if 'shelf' in recipe:
            mix = shelf_hi(mix, recipe['shelf']['hz'], recipe['shelf']['db'])
        if 'room' in recipe:
            mix = room(mix, rng, **recipe['room'])
        if tgt:
            mix = match(mix, tgt, recipe.get('match', 0.6))
        mix = fade(mix, recipe.get('fade_ms', 60))
        mix = mix / np.abs(mix).max() * 10 ** (recipe.get('peak_db', -1) / 20)
        path = os.path.join(out, f"{recipe['name']}_{v + 1:02d}.wav")
        sf.write(path, mix, SR, subtype='PCM_16')
        print(os.path.basename(path), 'bands', np.round(bands(mix, SR)).astype(int).tolist(), 'onsets', onsets(mix, SR))
    if tgt:
        print('target bands', np.round(tgt, 1).tolist())


if __name__ == '__main__':
    r = json.load(open(sys.argv[1]))
    build(r, sys.argv[2] if len(sys.argv) > 2 else f"D:/DaD_Sounds/Generated/{r['name']}")
