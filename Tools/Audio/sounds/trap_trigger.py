"""Scatto della trappola: la piastra cede, le punte escono, il ferro stride e batte nella pietra."""
import numpy as np

from sfxlib import bandpass, click, lowpass, metal_ping, mix_at, noise, reverb, silence, t_axis, thud

TITLE = "Scatto della trappola"
DESCRIPTION = "Due scatti metallici della piastra, le punte che escono, stridio di ferro e tonfo nella pietra."
COMPARE = ("impactMetal_light (Kenney)", "Kenney/impactMetal_light_000.ogg")
MAX_DUR = None


def build(rng):
    x = silence(1.6)
    mix_at(x, click(rng), 0.00, 0.8)
    mix_at(x, metal_ping(rng, [(1870, 1), (3120, 0.6), (4990, 0.3)], 0.25, 0.05), 0.00, 0.6)
    mix_at(x, click(rng), 0.07, 1.0)
    mix_at(x, metal_ping(rng, [(1650, 1), (2840, 0.7), (5300, 0.3)], 0.3, 0.06), 0.07, 0.7)
    dur = 0.18
    sweep = bandpass(noise(rng, dur), 700, 4000) * np.linspace(0.3, 1.0, len(t_axis(dur)))
    mix_at(x, 0.15 * sweep * np.sin(np.pi * t_axis(dur) / dur) ** 2, 0.16)
    mix_at(x, metal_ping(rng, [(1240, 1), (2210, 0.8), (3570, 0.6), (6100, 0.3)], 0.9, 0.18), 0.30, 0.9)
    mix_at(x, 0.45 * thud(70, 0.3, 0.07, drop=3.0), 0.30)
    mix_at(x, 0.3 * lowpass(click(rng, 0.03, 200, 2000), 800), 0.30)
    return reverb(rng, x, length=1.4, decay=0.35, wet=0.25)


SOUNDS = {"trap_trigger": build}
