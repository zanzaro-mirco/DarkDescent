"""Rintocco del santuario: campana di bronzo scura e un coro sottile che sale (il santuario si accende)."""
import numpy as np

from sfxlib import bell, lowpass, mix_at, reverb, silence, t_axis

TITLE = "Rintocco del santuario"
DESCRIPTION = "Campana di bronzo con battimenti, una quinta appena accennata e un coro sottile che sale."
COMPARE = ("level_up (il suono più vicino)", "Generated/level_up.wav")
MAX_DUR = 5.0


def build(rng):
    x = silence(4.5)
    mix_at(x, bell(rng, 196.0, 4.5), 0.0)          # sol2, scuro
    mix_at(x, bell(rng, 293.7, 3.5, 0.35), 0.0)    # la quinta sopra
    t = t_axis(3.0)
    swell = np.sin(np.pi * np.clip(t / 3.0, 0, 1)) ** 2
    pad = sum(np.sin(2 * np.pi * f * t) for f in (392.0, 392.9, 587.3, 783.0)) * swell
    mix_at(x, 0.08 * lowpass(pad, 2500), 0.15)
    return reverb(rng, x, length=3.0, decay=0.9, wet=0.35, damp=5000)


SOUNDS = {"shrine_chime": build}
