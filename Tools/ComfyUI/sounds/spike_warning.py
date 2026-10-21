"""Avviso degli spuntoni (D8 della M13): le punte che affiorano mezzo secondo prima di scattare."""

TITLE = "Avviso degli spuntoni (Stable Audio)"
DESCRIPTION = "Un meccanismo di pietra che gira e un raschio di ferro che sale: le punte stanno per uscire."
COMPARE = ("lo scatto della trappola", "Generated/trap_trigger_sa.wav")
MAX_DUR = None
SOUNDS = {
    "spike_warning_sa": {
        "prompt": ("Short stone mechanism grinding and a quiet rising iron scrape under the floor, "
                   "dungeon trap about to spring, close, dry"),
        "negative": "music, melody, speech, voice, low quality, distorted, explosion, thud",
        "seconds": 1.0,
        "seed": 1702,
    },
}
