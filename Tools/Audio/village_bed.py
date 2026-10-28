"""Il fondo del borgo (D10 della M18): il crepitio del fuoco dell'accampamento più il vento di Stable
Audio, chiuso ad anello sui 15 s del fuoco.

    python Tools/Audio/village_bed.py VENTO.wav [--out FILE] [--wind DB]

Il vento viene dalla ricetta village_wind di Tools/ComfyUI/sounds/village.py. L'anello: si tengono i
primi 15 s e la coda che segue si sfuma sopra l'inizio, a potenza costante, così la fine si attacca
all'inizio senza scatti. Il vento sta sotto il fuoco di --wind dB, misurati sul valore efficace.
"""
import argparse
import os

import numpy as np

import sfxlib

FIRE = os.path.join(sfxlib.ROOT, "Assets", "_Project", "Audio", "Ambience", "bed_town_fire.wav")
OUT = os.path.join(sfxlib.ROOT, "Assets", "_Project", "Audio", "Ambience", "bed_village.wav")
CROSSFADE = 2.5


def loop(x, length, crossfade):
    """I primi length campioni, con la coda dopo di loro sfumata sopra l'inizio."""
    if len(x) < length + crossfade:
        raise ValueError(f"servono {(length + crossfade) / sfxlib.SR:.1f} s, ce ne sono {len(x) / sfxlib.SR:.1f}")
    y = x[:length].copy()
    t = np.linspace(0, np.pi / 2, crossfade)
    y[:crossfade] = x[:crossfade] * np.sin(t) + x[length:length + crossfade] * np.cos(t)
    return y


def rms(x):
    return float(np.sqrt(np.mean(x ** 2)))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("wind", help="il WAV del vento")
    parser.add_argument("--out", default=OUT)
    parser.add_argument("--wind-db", type=float, default=-3.0, help="il vento rispetto al fuoco, in dB")
    args = parser.parse_args()

    fire = sfxlib.load(FIRE)
    wind = loop(sfxlib.load(args.wind), len(fire), int(CROSSFADE * sfxlib.SR))
    wind *= rms(fire) / rms(wind) * 10 ** (args.wind_db / 20)
    bed = fire + wind
    bed = bed / np.max(np.abs(bed)) * 10 ** (-1.0 / 20)
    sfxlib.write_wav(args.out, bed)
    print(f"[village_bed] {args.out}: {len(bed) / sfxlib.SR:.1f} s, vento {args.wind_db:+.0f} dB sul fuoco")


if __name__ == "__main__":
    main()
