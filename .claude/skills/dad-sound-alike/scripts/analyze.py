"""Numbers for comparing sounds by ear-relevant traits.

  py -I analyze.py <dir-or-glob> [<dir-or-glob> ...]

Per file: length, peak dBFS, band energies (dB relative to the loudest band; bands 20-150, 150-500, 500-1.5k,
1.5-4k, 4-10k, 10k+), onset times (s) and a 5 ms RMS envelope sampled every 20 ms. Each group ends with the mean
band profile - the 'target' a recipe can match.
"""
import glob, os, sys
import numpy as np, soundfile as sf
from scipy.signal import stft

BANDS = [(20, 150), (150, 500), (500, 1500), (1500, 4000), (4000, 10000), (10000, 24000)]


def load(f):
    x, sr = sf.read(f)
    return (x if x.ndim == 1 else x.mean(1)), sr


def bands(x, sr):
    fr, _, Z = stft(x, sr, nperseg=1024, noverlap=768)
    P = np.abs(Z) ** 2
    be = np.array([10 * np.log10(P[(fr >= a) & (fr < b)].sum() + 1e-12) for a, b in BANDS])
    return be - be.max()


def onsets(x, sr):
    _, t, Z = stft(x, sr, nperseg=1024, noverlap=768)
    env = 10 * np.log10((np.abs(Z) ** 2).sum(0) + 1e-12)
    d = np.diff(env)
    return [round(float(t[i + 1]), 2) for i in np.where((d > 8) & (env[1:] > env.max() - 25))[0]]


def envelope(x, sr, step_ms=20, upto_ms=400):
    w = int(sr * 0.005)
    e = 20 * np.log10(np.sqrt(np.convolve(x ** 2, np.ones(w) / w, 'same')) + 1e-9)
    return [int(max(e[i], -99)) for i in range(0, min(len(x), int(sr * upto_ms / 1000)), int(sr * step_ms / 1000))]


def files(arg):
    return sorted(glob.glob(os.path.join(arg, '*.wav')) if os.path.isdir(arg) else glob.glob(arg))


def target(arg):
    return np.mean([bands(*load(f)) for f in files(arg)], 0)


if __name__ == '__main__':
    for arg in sys.argv[1:]:
        print('==', arg)
        for f in files(arg):
            x, sr = load(f)
            print(f'  {os.path.basename(f)[:40]:40} {len(x) / sr:5.2f}s pk {20 * np.log10(abs(x).max() + 1e-9):4.0f}',
                  'bands', np.round(bands(x, sr)).astype(int).tolist(), 'onsets', onsets(x, sr))
            print('    env', envelope(x, sr))
        print('  mean bands', np.round(target(arg), 1).tolist())
