"""Ricetta per Stable Audio 3 Small SFX, da ascoltare accanto alla versione da script (passo 13.4)."""

TITLE = "Barile che si rompe (Stable Audio)"
DESCRIPTION = "Lo stesso barile della versione da script, chiesto a Stable Audio 3 Small SFX."
COMPARE = ("da script, sintetizzato", "Generated/barrel_break_0.wav")
MAX_DUR = None
SOUNDS = {
    "barrel_break_sa": {
        "prompt": ("Wooden barrel smashed open by a heavy blow, dry oak staves cracking and splintering, "
                   "an iron hoop clanging on a stone floor, small wood debris bouncing, dungeon room, "
                   "close"),
        "negative": "music, melody, speech, voice, low quality, distorted, noise hiss",
        "seconds": 2.5,
        "seed": 1301,
    },
}
