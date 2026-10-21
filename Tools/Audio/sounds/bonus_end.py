"""Fine del bonus del santuario: due rintocchi piccoli che scendono e un soffio che si spegne (la magia se ne va)."""
import numpy as np

from sfxlib import SR, bell, lowpass, mix_at, reverb, silence, t_axis

TITLE = "Fine del bonus del santuario"
DESCRIPTION = "Due campanelle che scendono di una quarta, più piccole del rintocco del santuario, e un coro che cala."
COMPARE = ("shrine_chime (il santuario che si accende)", "Generated/shrine_chime.wav")
MAX_DUR = 2.5


def build(rng):
    x = silence(2.2)
    mix_at(x, bell(rng, 587.3, 1.6, 0.55), 0.0)    # re4
    mix_at(x, bell(rng, 440.0, 1.9, 0.5), 0.22)    # la3, una quarta sotto
    t = t_axis(1.4)
    fall = 1.0 - 0.25 * np.clip(t / 1.4, 0, 1)      # il coro scende di un quarto
    phase = 2 * np.pi * np.cumsum(fall) / SR
    pad = sum(np.sin(f * phase) for f in (523.3, 524.1, 784.0)) * np.exp(-t * 2.2)
    mix_at(x, 0.06 * lowpass(pad, 2200), 0.05)
    return reverb(rng, x, length=1.8, decay=0.7, wet=0.3, damp=4500)


SOUNDS = {"bonus_end": build}
