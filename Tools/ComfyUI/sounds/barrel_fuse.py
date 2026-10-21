"""Miccia del barile esplosivo (D8 della M13): un secondo di sfrigolio prima dello scoppio."""

TITLE = "Miccia del barile esplosivo (Stable Audio)"
DESCRIPTION = "Una miccia accesa che sfrigola e scoppietta per un secondo."
COMPARE = ("il barile che si rompe", "Generated/barrel_break_sa.wav")
MAX_DUR = None
SOUNDS = {
    "barrel_fuse_sa": {
        "prompt": "A lit black powder fuse hissing and sputtering with small crackling sparks, close, dry",
        "negative": "music, melody, speech, voice, low quality, explosion, thud, wind",
        "seconds": 1.2,
        "seed": 1704,
    },
}
