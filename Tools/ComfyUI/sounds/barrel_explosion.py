"""Esplosione del barile esplosivo (D8 della M13): il barile salta in aria in una stanza di pietra."""

TITLE = "Esplosione del barile (Stable Audio)"
DESCRIPTION = "Un barile di polvere nera che esplode in una cripta: botto, legno che vola, eco di pietra."
COMPARE = ("il barile che si rompe", "Generated/barrel_break_sa.wav")
MAX_DUR = None
SOUNDS = {
    "barrel_explosion_sa": {
        "prompt": ("A wooden barrel of black powder explodes in a stone dungeon room: a deep boom, "
                   "wood splinters flying, debris falling, short stone echo"),
        "negative": "music, melody, speech, voice, low quality, gunshot, sci-fi, laser",
        "seconds": 2.5,
        "seed": 1703,
    },
}
