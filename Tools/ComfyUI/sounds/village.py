"""Ricette per Stable Audio 3 Small SFX: il fondo del borgo (D10 della M18). Il vento si mescola con il
crepitio del fuoco di prima in un fondo solo, chiuso ad anello da Tools/Audio/village_bed.py; i corvi
sono i versi lontani che il borgo fa sentire ogni tanto, come i versi nel buio della cripta."""

TITLE = "Il fondo del borgo (Stable Audio)"
DESCRIPTION = "Il vento tra gli alberi e i corvi lontani del borgo, da mescolare con il fuoco dell'accampamento."
COMPARE = ("il fuoco di prima", "../Ambience/bed_town_fire.wav")
MAX_DUR = 18.0
SOUNDS = {
    "village_wind": {
        "prompt": ("Gentle steady wind blowing through pine trees and over an empty village at dusk, soft "
                   "low gusts, rustling needles, calm outdoor ambience, no birds, seamless"),
        "negative": "music, melody, speech, voice, birds, rain, thunder, footsteps, low quality, distorted",
        "seconds": 18.0,
        "seed": 1851,
    },
    "crow_caw": {
        "prompt": ("A single crow cawing two or three times far away across an open field, distant, "
                   "outdoor, natural reverb"),
        "negative": "music, speech, voice, wind, rain, many birds, low quality, distorted",
        "seconds": 3.0,
        "seed": 1855,
    },
}
