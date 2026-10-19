"""Barile che si rompe, in due varianti che il gioco alterna a caso (D14).

L'ibrida parte dallo schianto di ossa di Puadlung: il WAV che ne esce ha la licenza di quel pacchetto
(si usa nel gioco, non si ridistribuisce da solo; ADR-070).
"""
import os

import numpy as np

from sfxlib import SFX, load, lowpass, metal_ping, mix_at, pitch, reverb, silence, thud, wood_crack

TITLE = "Barile che si rompe"
DESCRIPTION = "Tonfo del corpo, raffica di schiocchi di legno, cerchio di ferro, schegge che rimbalzano."
COMPARE = ("impactPlank_medium (Kenney, non usato)", "Kenney/impactPlank_medium_000.ogg")
MAX_DUR = None
BONE_HIT = os.path.join(SFX, "Puadlung", "RKN_Monster_Skeleton_BoneHit_01.wav")


def debris(rng, x, start, count):
    """Pezzi che cadono e rimbalzano sulla pietra."""
    for at in start + np.cumsum(rng.exponential(0.07, count)):
        mix_at(x, wood_crack(rng, 0.025, rng.uniform(0.08, 0.3), rng.uniform(1.4, 2.4)), at)


def build_synth(rng):
    x = silence(1.8)
    mix_at(x, 1.2 * thud(95, 0.4, 0.09), 0.0)
    for at in np.cumsum(rng.exponential(0.025, 9)):
        mix_at(x, wood_crack(rng, rng.uniform(0.05, 0.12), rng.uniform(0.5, 1.0), rng.uniform(0.8, 1.3)), at)
    mix_at(x, 0.12 * metal_ping(rng, [(820, 1), (1960, 0.6), (3450, 0.3)], 0.8, 0.2), 0.22)
    debris(rng, x, 0.3, 14)
    return reverb(rng, x, length=1.2, decay=0.3, wet=0.22)


def build_hybrid(rng):
    x = silence(1.8)
    mix_at(x, 0.7 * lowpass(pitch(load(BONE_HIT), 0.7), 5000), 0.0)
    mix_at(x, thud(95, 0.4, 0.09), 0.0)
    for at in np.cumsum(rng.exponential(0.03, 6)):
        mix_at(x, wood_crack(rng, rng.uniform(0.05, 0.1), rng.uniform(0.3, 0.6)), at)
    debris(rng, x, 0.3, 12)
    return reverb(rng, x, length=1.2, decay=0.3, wet=0.22)


SOUNDS = {"barrel_break_0": build_synth, "barrel_break_1": build_hybrid}
