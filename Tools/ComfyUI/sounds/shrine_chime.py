"""Ricetta per Stable Audio 3 Small SFX, da ascoltare accanto alla versione da script (passo 13.4)."""

TITLE = "Rintocco del santuario (Stable Audio)"
DESCRIPTION = "Il rintocco del santuario della versione da script, chiesto a Stable Audio 3 Small SFX."
COMPARE = ("da script", "Generated/shrine_chime.wav")
MAX_DUR = 5.0
SOUNDS = {
    "shrine_chime_sa": {
        "prompt": ("Single deep bronze temple bell struck once, warm shimmering overtones, faint "
                   "ethereal choir rising underneath, slow fade in a stone crypt, magical blessing"),
        "negative": "music, melody, speech, voice, low quality, distorted, noise hiss",
        "seconds": 5.0,
        "seed": 1302,
    },
}
