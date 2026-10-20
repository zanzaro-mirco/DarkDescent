"""Ricetta per Stable Audio 3 Small SFX, da ascoltare accanto alla versione da script (passo 13.4)."""

TITLE = "Sorso dal pozzo (Stable Audio)"
DESCRIPTION = "Il sorso dal pozzo della versione da script, chiesto a Stable Audio 3 Small SFX."
COMPARE = ("da script", "Generated/well_drink.wav")
MAX_DUR = None
SOUNDS = {
    "well_drink_sa": {
        "prompt": ("Wooden bucket dipped into a stone well, water splashing, three quick gulps of "
                   "drinking, drops falling back into the water with a soft echo"),
        "negative": "music, melody, speech, voice, low quality, distorted, noise hiss",
        "seconds": 3.0,
        "seed": 1304,
    },
}
