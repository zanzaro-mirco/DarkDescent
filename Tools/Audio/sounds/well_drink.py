"""Sorso dal pozzo: il secchio entra nell'acqua, tre sorsi, gocce che ricadono lontane."""
import numpy as np

from sfxlib import SR, bandpass, bubble, env_exp, lowpass, mix_at, noise, overlay, reverb, silence, t_axis

TITLE = "Sorso dal pozzo"
DESCRIPTION = "Secchio nell'acqua (bolle di Minnaert), tre sorsi, gocce che ricadono lontane."
COMPARE = ("pozione bevuta (Puadlung)", "Puadlung/RKN_Item_Potion_Drink_01.wav")
MAX_DUR = None


def build(rng):
    x = silence(2.4)
    mix_at(x, 0.4 * bandpass(noise(rng, 0.35), 400, 5000) * env_exp(0.35, 0.08, 0.004), 0.0)
    for at in np.cumsum(rng.exponential(0.018, 22)):
        mix_at(x, bubble(rng.uniform(2.5, 7), rng.uniform(0.3, 0.9)), at)
    for at in (0.75, 1.2, 1.65):
        gt = t_axis(0.12)
        gulp = np.sin(2 * np.pi * np.cumsum(180 - 500 * gt) / SR) * env_exp(0.12, 0.035, 0.01)
        mix_at(x, 0.6 * lowpass(gulp, 900), at)
        for b in at + np.cumsum(rng.exponential(0.012, 7)):
            mix_at(x, bubble(rng.uniform(1.5, 4), rng.uniform(0.2, 0.5)), b)
    drops = silence(2.4)
    for at in (1.75, 1.9, 2.08):
        mix_at(drops, bubble(rng.uniform(2, 3.5), 0.25), at)
    near = reverb(rng, lowpass(x, 7000), length=0.8, decay=0.18, wet=0.1)
    far = reverb(rng, drops, length=1.6, decay=0.5, wet=0.45)
    return overlay(near, far, 0.8)


SOUNDS = {"well_drink": build}
