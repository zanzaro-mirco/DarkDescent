"""Ricetta per Stable Audio 3 Small SFX, da ascoltare accanto alla versione da script (passo 13.4)."""

TITLE = "Sorso dal pozzo (Stable Audio)"
DESCRIPTION = "Il sorso dal pozzo della versione da script, chiesto a Stable Audio 3 Small SFX."
COMPARE = ("da script", "Generated/well_drink.wav")
MAX_DUR = None
SOUNDS = {
    "well_drink_sa": {
        "prompt": ("Clear water splashing in a stone well: a wooden bucket plunges into the water with a "
                   "bright splash, water sloshes and pours from the bucket, then drops trickle back and "
                   "drip into the well, wet and close, echoing stone shaft"),
        "negative": "music, melody, speech, voice, low quality, distorted, noise hiss",
        "seconds": 3.5,
        "seed": 1402,
    },
}
