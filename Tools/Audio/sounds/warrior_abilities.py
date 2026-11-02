"""Le abilità del Guerriero (M20): il turbine della lama, lo scudo che sbatte, l'atterraggio del Salto e le stelle dello stordimento."""
import numpy as np

from sfxlib import bandpass, bell, click, lowpass, metal_ping, mix_at, noise, reverb, silence, t_axis, thud

TITLE = "Abilità del Guerriero"
DESCRIPTION = "Turbine: la lama che fischia in cerchio. Scudo: il ferro che sbatte con un colpo sordo. Salto: l'atterraggio pesante. Stordimento: due tintinnii che girano."
COMPARE = ("impactMetal_light (Kenney)", "Kenney/impactMetal_light_000.ogg")
MAX_DUR = 2.0


def whirl(rng):
    # un fischio che sale e scende due volte: la lama che passa davanti e dietro
    dur = 0.7
    t = t_axis(dur)
    x = silence(1.0)
    hiss = bandpass(noise(rng, dur), 900, 5200)
    sweep = np.sin(np.pi * t / dur * 2.0) ** 2 * np.sin(np.pi * t / dur)
    mix_at(x, 0.5 * hiss * sweep, 0.0)
    ring = metal_ping(rng, [(2650, 1), (4100, 0.5)], 0.4, 0.12)
    mix_at(x, 0.12 * ring, 0.18)
    return reverb(rng, x, length=0.8, decay=0.3, wet=0.2)


def shield(rng):
    # il bordo dello scudo che batte: un ferro grave e un tonfo di legno sotto
    x = silence(0.9)
    mix_at(x, click(rng, 0.015, 1500, 7000), 0.0, 0.9)
    mix_at(x, metal_ping(rng, [(620, 1), (1340, 0.7), (2210, 0.45), (3480, 0.2)], 0.6, 0.11), 0.0, 0.8)
    mix_at(x, 0.7 * thud(95, 0.25, 0.05, drop=2.5), 0.0)
    return reverb(rng, x, length=0.9, decay=0.3, wet=0.22)


def land(rng):
    # i piedi corazzati che piombano a terra, la polvere e il ferro che trema
    x = silence(1.2)
    mix_at(x, 0.9 * thud(55, 0.45, 0.09, drop=3.0), 0.0)
    dust = lowpass(noise(rng, 0.5), 900) * np.exp(-t_axis(0.5) / 0.12)
    mix_at(x, 0.5 * dust, 0.0)
    mix_at(x, 0.25 * metal_ping(rng, [(880, 1), (1610, 0.6)], 0.35, 0.08), 0.02)
    return reverb(rng, x, length=1.2, decay=0.4, wet=0.25)


def stun(rng):
    # due tintinnii alti che si rincorrono: le stelle sopra la testa
    x = silence(1.4)
    mix_at(x, bell(rng, 1568.0, 0.9, 0.5), 0.0)
    mix_at(x, bell(rng, 2093.0, 0.8, 0.4), 0.12)
    mix_at(x, bell(rng, 1760.0, 0.7, 0.3), 0.26)
    return reverb(rng, x, length=1.0, decay=0.4, wet=0.3)


SOUNDS = {
    "ability_whirl": whirl,
    "ability_shield": shield,
    "ability_land": land,
    "ability_stun": stun,
}
