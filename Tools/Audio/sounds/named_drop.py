"""Un unico o un pezzo di set tocca terra (D15 della M16): un rintocco caldo per l'oro, uno più chiaro per il verde."""
import numpy as np

from sfxlib import bell, env_exp, lowpass, mix_at, reverb, silence, t_axis

TITLE = "Unico o set a terra"
DESCRIPTION = ("Unico: campana bassa e un luccichio che sale, sopra il tintinnio di sempre. "
               "Set: tre note chiare in arpeggio, più leggere.")
COMPARE = ("shrine_chime (il suono più vicino)", "Generated/shrine_chime.wav")
MAX_DUR = 3.0


def shimmer(rng, start, end, dur):
    """Un luccichio: note brevi e acute sempre più alte, come monete che brillano."""
    x = silence(dur)
    count = 7
    for i in range(count):
        f = start * (end / start) ** (i / (count - 1)) * (1 + rng.uniform(-0.01, 0.01))
        t = t_axis(0.35)
        note = np.sin(2 * np.pi * f * t) * env_exp(0.35, 0.09)
        mix_at(x, 0.12 * note, 0.05 + i * 0.07)
    return x


def unique(rng):
    x = silence(2.6)
    mix_at(x, bell(rng, 261.6, 2.6, 0.9), 0.0)      # do3, caldo
    mix_at(x, bell(rng, 392.0, 2.0, 0.3), 0.0)      # la quinta
    mix_at(x, lowpass(shimmer(rng, 1046.5, 2093.0, 1.2), 6000), 0.1)
    return reverb(rng, x, length=2.4, decay=0.8, wet=0.3, damp=5500)


def set_piece(rng):
    x = silence(2.2)
    for i, f in enumerate((659.3, 830.6, 987.8)):   # mi, sol diesis, si: un arpeggio maggiore
        mix_at(x, bell(rng, f, 1.6, 0.45), i * 0.09)
    mix_at(x, lowpass(shimmer(rng, 1318.5, 2637.0, 0.9), 7000), 0.25)
    return reverb(rng, x, length=2.0, decay=0.7, wet=0.3, damp=6000)


SOUNDS = {"unique_drop": unique, "set_drop": set_piece}
