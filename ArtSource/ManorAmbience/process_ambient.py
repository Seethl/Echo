"""Non-destructive ambience edit. Python standard library + NumPy only."""
from pathlib import Path
import hashlib
import json
import math
import shutil
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[1]
SOURCE = ROOT / 'igor ambient.wav'
OUTPUT = REPO / 'Assets/Audio/ManorAmbience/ManorAmbientLoop.wav'
PREVIEW_OUTPUT = REPO / 'work/manor_ambience/output/ManorAmbientLoop_Edited.wav'
# The earlier loop skipped 1.5 s of the unmodified team recording. These
# original-source times correspond to 18.83..146.00 s in that former loop.
TRIM_SECONDS = 20.33
TRIM_END_SECONDS = 147.5
# Preserve the reference recording gain used for the earlier in-game clip.
SOURCE_GAIN = 1.3895867690712906
LOOP_CROSSFADE_SECONDS = 2.0
WET_MIX = 0.12


def sha256(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def read_wav(p):
    with wave.open(str(p), 'rb') as w:
        assert w.getsampwidth() == 2 and w.getnchannels() == 2
        sr = w.getframerate()
        x = np.frombuffer(w.readframes(w.getnframes()), '<i2').reshape(-1, 2)
    return x.astype(np.float32) / 32768, sr


def convolve(x, ir, block=65536):
    """FFT overlap-add, bounded memory and no circular wraparound."""
    size = 1 << (block + len(ir) - 2).bit_length()
    hf = np.fft.rfft(ir, n=size)
    out = np.zeros(len(x) + len(ir) - 1, dtype=np.float32)
    for start in range(0, len(x), block):
        part = x[start:start + block]
        z = np.fft.irfft(np.fft.rfft(part, n=size) * hf, n=size)
        count = len(part) + len(ir) - 1
        out[start:start + count] += z[:count].astype(np.float32)
    return out


def tone_filter(sr):
    # Subtle broad EQ: remove subsonic rumble, ease low-mid buildup and hard highs.
    size = 16384
    f = np.fft.rfftfreq(size, 1 / sr)
    hp = f ** 2 / (f ** 2 + 27 ** 2)
    low_shelf = -0.65 / (1 + (f / 190) ** 2)
    presence = -1.25 * np.exp(-0.5 * (np.log2(np.maximum(f, 1) / 2900) / 0.85) ** 2)
    high_shelf = -0.8 * f ** 2 / (f ** 2 + 7200 ** 2)
    mag = hp * 10 ** ((low_shelf + presence + high_shelf) / 20)
    impulse = np.fft.fftshift(np.fft.irfft(mag, n=size))
    half = 4096
    return (impulse[size//2-half:size//2+half+1] * np.hanning(half*2+1)).astype(np.float32)


def smooth_dynamics(x, sr):
    hop = round(sr * 0.01)
    pad = (-len(x)) % hop
    padded = np.pad(x, ((0, pad), (0, 0)))
    rms = np.sqrt(np.mean(padded.reshape(-1, hop, 2) ** 2, axis=(1, 2)))
    # Use 50 ms energy blocks, with quiet ends replicated.
    rms = np.sqrt(np.convolve(np.pad(rms ** 2, (2, 2), mode='edge'), np.ones(5)/5, mode='valid'))
    level = 20 * np.log10(np.maximum(rms, 1e-8))
    threshold, ratio, knee = -22.0, 1.4, 6.0
    over = level - threshold
    reduction = np.where(over <= -knee/2, 0,
                         np.where(over >= knee/2, (1 - 1/ratio) * over,
                                  (1 - 1/ratio) * (over + knee/2)**2 / (2*knee)))
    target = np.minimum(reduction, 3.0)
    state = 0.0
    smoothed = np.empty_like(target)
    attack, release = math.exp(-0.01/0.06), math.exp(-0.01/0.7)
    for i, value in enumerate(target):
        a = attack if value > state else release
        state = a*state + (1-a)*value
        smoothed[i] = state
    control = np.interp(np.arange(len(x)), np.arange(len(target))*hop + hop/2, smoothed)
    return (x * (10 ** (-control/20))[:, None]).astype(np.float32), float(smoothed.max())


def reverb_ir(sr, seed, rt60):
    rng = np.random.default_rng(seed)
    n = round(2.7*sr)
    t = np.arange(n)/sr
    noise = rng.standard_normal(n).astype(np.float32)
    # Damped and diffuse late tail, not a series of obvious repeated echoes.
    taps = 129
    k = np.arange(taps) - (taps-1)/2
    low = 2 * 4400/sr * np.sinc(2*4400/sr*k) * np.hanning(taps)
    low /= low.sum()
    late = np.convolve(noise, low, mode='same')
    onset = np.clip((t-.065)/.055, 0, 1)
    late *= onset * np.exp(-math.log(1000)*np.maximum(t-.065, 0)/rt60)
    late *= 0.78/max(np.linalg.norm(late), 1e-8)
    for delay, amp in [(0.024, .31), (0.037, .23), (.053, .18), (.071, .13), (.089, .09)]:
        pos = round((delay + rng.uniform(-.0015, .0015))*sr)
        late[pos] += amp
    late[-round(.12*sr):] *= np.linspace(1, 0, round(.12*sr))
    return late.astype(np.float32)


def stats(x, sr):
    return dict(seconds=round(len(x)/sr, 4), sample_rate=sr, channels=x.shape[1],
                peak_dbfs=round(float(20*np.log10(max(float(abs(x).max()), 1e-12))), 3),
                rms_dbfs=round(float(20*np.log10(max(float(np.sqrt(np.mean(x*x))), 1e-12))), 3),
                clipped_samples=int(np.count_nonzero(abs(x)>=1)),
                stereo_correlation=round(float(np.corrcoef(x[::8, 0], x[::8, 1])[0, 1]), 4))


def loop_crossfade(x, sr):
    count = round(LOOP_CROSSFADE_SECONDS*sr)
    assert len(x) > count*3
    # Smooth complementary windows. Finish on the original head[count-1],
    # followed by head[count] at the start of the next cycle.
    t = np.linspace(0, math.pi/2, count, dtype=np.float32)[:, None]
    tail_weight, head_weight = np.cos(t), np.sin(t)
    a, b = x[-count:], x[:count]
    correlation = float(np.corrcoef(a[::8].reshape(-1), b[::8].reshape(-1))[0, 1])
    normalizer = np.sqrt(tail_weight**2 + head_weight**2 +
                         2*max(0.0, correlation)*tail_weight*head_weight)
    seam = (a*tail_weight + b*head_weight)/normalizer
    return np.concatenate((x[count:-count], seam)), correlation


def periodic_reverb(feed, ir):
    # Fold the decay into the beginning, making the reverb continuous across
    # every repeat instead of appending a tail and silence to the WAV.
    result = convolve(feed, ir)
    n = len(feed)
    result[:len(ir)-1] += result[n:]
    return result[:n]


def write_wav(p, x, sr):
    p.parent.mkdir(parents=True, exist_ok=True)
    # Deterministic TPDF dither, then 16-bit PCM with ample peak headroom.
    rng = np.random.default_rng(20261101)
    dither = (rng.random(x.shape, dtype=np.float32) - rng.random(x.shape, dtype=np.float32))/32768
    pcm = np.rint((x+dither)*32768).clip(-32768, 32767).astype('<i2')
    with wave.open(str(p), 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr)
        w.writeframes(pcm.tobytes())


def main():
    source_hash = sha256(SOURCE)
    assert source_hash == '723288895a58a02a0f7ebeb497cc425a534d8981c05290dc221f981c6a628231', 'Unexpected source recording'
    raw, sr = read_wav(SOURCE)
    dry = raw[round(TRIM_SECONDS*sr):round(TRIM_END_SECONDS*sr)].copy() * SOURCE_GAIN
    ir = tone_filter(sr)
    delay = (len(ir)-1)//2
    dry = np.column_stack([convolve(dry[:, ch], ir)[delay:delay+len(dry)] for ch in range(2)])
    dry, reduction = smooth_dynamics(dry, sr)
    dry, seam_correlation = loop_crossfade(dry, sr)
    wet = []
    for ch in range(2):
        feed = .88*dry[:, ch] + .12*dry[:, 1-ch]
        wet.append(periodic_reverb(feed, reverb_ir(sr, 1485+ch, 1.85+ch*.12)))
    wet = np.column_stack(wet)
    # Match reverb energy before blending, measured away from the opening.
    region = slice(round(3*sr), min(len(dry), round(70*sr)))
    wet *= np.sqrt(np.mean(dry[region]**2))/max(float(np.sqrt(np.mean(wet[region]**2))), 1e-8)
    processed = (1-WET_MIX)*dry + WET_MIX*wet
    peak = float(np.max(abs(processed)))
    gain = min(1.0, 10**(-2.0/20)/max(peak, 1e-8))
    processed *= gain
    write_wav(OUTPUT, processed, sr)
    PREVIEW_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(OUTPUT, PREVIEW_OUTPUT)
    decoded, sr2 = read_wav(OUTPUT)
    assert sr2 == sr and np.isfinite(decoded).all()
    assert float(abs(decoded).max()) <= 10**(-1.99/20)
    assert np.sqrt(np.mean(decoded[:round(.1*sr)]**2)) > .001, 'Unexpected silent intro'
    assert sha256(SOURCE) == source_hash, 'Original modified'
    report = {
        'source': str(SOURCE), 'source_sha256': source_hash, 'output_path': str(OUTPUT),
        'output_sha256': sha256(OUTPUT), 'trim_start_seconds': TRIM_SECONDS,
        'trim_end_seconds': TRIM_END_SECONDS,
        'discarded_tail_seconds': round(len(raw)/sr-TRIM_END_SECONDS, 3),
        'loop_crossfade_seconds': LOOP_CROSSFADE_SECONDS,
        'loop_seam_source_correlation': round(seam_correlation, 4),
        'boundary_step': float(np.max(np.abs(decoded[0]-decoded[-1]))),
        'source_gain': SOURCE_GAIN, 'reverb_wet_mix': WET_MIX, 'reverb_rt60_seconds': [1.85, 1.97],
        'reverb_added_tail_seconds': round((len(processed)-len(dry))/sr, 3),
        'max_compression_db': round(reduction, 3),
        'final_gain_db': round(float(20*np.log10(gain)), 3),
        'input': stats(raw, sr), 'output': stats(decoded, sr),
        'checks': {'original_unchanged': True, 'finite_samples': True,
                   'no_clipping': True, 'no_long_silent_intro': True,
                   'stereo_and_sample_rate_preserved': True},
        'audition': 'Signal-level checks only. No subjective listening or Unity playback check.',
    }
    (ROOT/'processing-report.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
    print(json.dumps(report, indent=2, ensure_ascii=False))


if __name__ == '__main__':
    main()
