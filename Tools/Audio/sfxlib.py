"""Libreria comune dei suoni fatti da script (M13, D14).

Ogni funzione che usa il caso riceve il generatore `rng`: ogni suono ha il suo seme, così un WAV si
rifà identico e cambiare un suono non cambia gli altri.
"""
import os
import zlib

import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SFX = os.path.join(ROOT, "Assets", "_Project", "Audio", "SFX")
GENERATED = os.path.join(SFX, "Generated")


def rng_for(name):
    """Il generatore di un suono: il seme viene dal nome, non dall'ordine in cui si generano."""
    return np.random.default_rng(zlib.crc32(name.encode("utf-8")))


# ---------- tempo e inviluppi ----------

def t_axis(dur):
    return np.arange(int(dur * SR)) / SR


def silence(dur):
    return np.zeros(int(dur * SR))


def env_exp(dur, decay, attack=0.002):
    """Attacco lineare e coda esponenziale."""
    t = t_axis(dur)
    a = np.clip(t / attack, 0, 1) if attack > 0 else 1
    return a * np.exp(-t / decay)


def noise(rng, dur):
    return rng.standard_normal(int(dur * SR))


# ---------- filtri ----------

def bandpass(x, lo, hi, order=2):
    return signal.sosfilt(signal.butter(order, [lo, hi], btype="band", fs=SR, output="sos"), x)


def lowpass(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, btype="low", fs=SR, output="sos"), x)


def highpass(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, btype="high", fs=SR, output="sos"), x)


def resonator(x, freq, q):
    """Filtro risonante a due poli: fa suonare un eccitatore alla frequenza data."""
    b, a = signal.iirpeak(freq, q, fs=SR)
    return signal.lfilter(b, a, x)


# ---------- montaggio ----------

def mix_at(dst, src, at, gain=1.0):
    """Somma src dentro dst a partire dal secondo at, tagliando quello che esce."""
    i = int(at * SR)
    end = min(len(dst), i + len(src))
    if end > i:
        dst[i:end] += gain * src[: end - i]
    return dst


def overlay(a, b, gain_b=1.0):
    """Somma due segnali di lunghezza diversa."""
    n = max(len(a), len(b))
    return np.pad(a, (0, n - len(a))) + gain_b * np.pad(b, (0, n - len(b)))


def reverb(rng, x, length=1.6, decay=0.45, wet=0.3, damp=4000, predelay=0.012):
    """Riverbero a convoluzione con una risposta all'impulso di rumore smorzato (la cripta)."""
    t = t_axis(length)
    ir = lowpass(rng.standard_normal(len(t)) * np.exp(-t / decay), damp)
    ir = np.concatenate([silence(predelay), ir])
    ir /= np.sqrt(np.sum(ir ** 2))
    tail = signal.fftconvolve(x, ir)
    return (1 - wet) * np.pad(x, (0, len(tail) - len(x))) + wet * tail


# ---------- mattoni sonori ----------

def click(rng, dur=0.012, lo=2500, hi=9000):
    """Uno scatto secco: rumore filtrato con una coda brevissima."""
    return bandpass(noise(rng, dur), lo, hi) * env_exp(dur, dur / 4, 0.0005)


def metal_ping(rng, freqs, dur, decay):
    """Un pezzo di ferro colpito: risonatori stretti (frequenza, guadagno) eccitati da uno scatto."""
    exc = click(rng, 0.004, 1000, 12000)
    x = silence(dur)
    x[: len(exc)] = exc
    out = sum(resonator(x, f, 60) * g for f, g in freqs) * np.exp(-t_axis(dur) / decay)
    return out / np.max(np.abs(out))


def thud(freq, dur, decay, drop=2.0):
    """Un tonfo: sinusoide grave che scende di tono."""
    t = t_axis(dur)
    return np.sin(2 * np.pi * freq * t * np.exp(-t * drop)) * env_exp(dur, decay)


BELL_PARTIALS = [(0.5, 0.45, 2.2), (1.0, 0.8, 2.4), (1.19, 0.6, 1.6), (1.5, 0.45, 1.2),
                 (2.0, 0.7, 0.9), (2.5, 0.22, 0.6), (2.66, 0.18, 0.5), (3.01, 0.15, 0.4),
                 (4.17, 0.08, 0.25), (5.43, 0.05, 0.15)]


def bell(rng, f0, dur, gain=1.0):
    """Campana di bronzo: parziali inarmonici (rapporto, ampiezza, coda) con battimenti e batacchio."""
    t = t_axis(dur)
    x = np.zeros(len(t))
    for ratio, amp, dec in BELL_PARTIALS:
        f = f0 * ratio * (1 + rng.uniform(-0.002, 0.002))
        beat = 1 + 0.15 * np.sin(2 * np.pi * rng.uniform(0.6, 2.2) * t)
        x += amp * beat * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t / dec)
    strike = bandpass(noise(rng, 0.03), 1500, 6000) * env_exp(0.03, 0.006)
    x[: len(strike)] += 0.25 * strike
    return gain * x


WOOD_MODES = [(240, 8), (510, 9), (870, 10), (1430, 12), (2300, 14)]


def wood_crack(rng, dur=0.09, gain=1.0, shift=1.0):
    """Uno schiocco di legno: rumore che fa suonare i modi di una doga, più la scheggia."""
    exc = noise(rng, dur) * env_exp(dur, dur / 5, 0.0008)
    x = sum(resonator(exc, f * shift * rng.uniform(0.93, 1.07), q) * rng.uniform(0.5, 1.0)
            for f, q in WOOD_MODES)
    return gain * (x + 0.35 * highpass(exc, 2500))


def bubble(radius_mm, gain=1.0):
    """Bolla di Minnaert: sinusoide smorzata la cui frequenza sale mentre la bolla risale."""
    f0 = 3260.0 / radius_mm
    dur = 0.06 + 0.02 * radius_mm
    t = t_axis(dur)
    f = f0 * (1 + 2.5 * t / dur)
    phase = 2 * np.pi * np.cumsum(f) / SR
    return gain * np.sin(phase) * np.exp(-t / (0.012 + 0.006 * radius_mm)) * np.clip(t / 0.001, 0, 1)


# ---------- sample ----------

def load(path):
    """Un WAV del progetto, mono, normalizzato e alla frequenza di lavoro."""
    import warnings
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", wavfile.WavFileWarning)
        rate, d = wavfile.read(path)
    d = d.astype(np.float64)
    if d.ndim > 1:
        d = d.mean(axis=1)
    d /= np.max(np.abs(d))
    return signal.resample_poly(d, SR, rate) if rate != SR else d


def pitch(x, factor):
    """Cambio di velocità, come un nastro: factor < 1 abbassa e allunga."""
    return signal.resample_poly(x, 100, int(round(100 * factor)))


# ---------- uscita ----------

def fade_out(x, dur=0.05):
    n = min(len(x), int(dur * SR))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def finish(x, peak_db=-1.0, max_dur=None, thresh=1e-3):
    """Toglie il rombo sotto i 40 Hz e il silenzio in coda, taglia a max_dur con una dissolvenza,
    normalizza al picco."""
    x = highpass(x, 40)
    idx = np.nonzero(np.abs(x) > thresh * np.max(np.abs(x)))[0]
    x = x[: idx[-1] + 1]
    if max_dur is not None and len(x) > max_dur * SR:
        x = fade_out(x[: int(max_dur * SR)], 1.0)
    x = fade_out(x)
    return x / np.max(np.abs(x)) * 10 ** (peak_db / 20)


def write_wav(path, x):
    wavfile.write(path, SR, (np.clip(x, -1, 1) * 32767).astype(np.int16))
