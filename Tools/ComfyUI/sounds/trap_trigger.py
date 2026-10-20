"""Ricetta per Stable Audio 3 Small SFX, da ascoltare accanto alla versione da script (passo 13.4)."""

TITLE = "Scatto della trappola (Stable Audio)"
DESCRIPTION = "Lo scatto della trappola della versione da script, chiesto a Stable Audio 3 Small SFX."
COMPARE = ("da script", "Generated/trap_trigger.wav")
MAX_DUR = None
SOUNDS = {
    "trap_trigger_sa": {
        "prompt": ("Stone pressure plate clicks twice, then sharp iron spikes shoot up out of the floor "
                   "with a metallic scrape and a heavy thud, dungeon trap, close"),
        "negative": "music, melody, speech, voice, low quality, distorted, noise hiss",
        "seconds": 1.5,
        "seed": 1303,
    },
}
