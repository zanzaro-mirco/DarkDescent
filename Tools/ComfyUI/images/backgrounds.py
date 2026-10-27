"""Gli sfondi a tutto schermo di menu, morte e vittoria (M17, D6): illustrazioni in stile diorama low poly,
come i modelli KayKit, scurite dove stanno i testi. Negli asset vanno con

    python Tools/ComfyUI/make.py menu_back death_back --out Assets/_Project/Art/Generated/Backgrounds

La vittoria invece è un render del gioco (la riserva della D6): l'accampamento all'alba generato
sembrava di un altro gioco, e Mirco l'ha scartato il 27 ott 2026. Il test PhotoTour.VictoryBackdrop
fotografa la sala del re appena abbattuto in Recordings/Photos/victory_render.png, e

    python Tools/ComfyUI/renders.py

lo rifinisce con la ricetta di RENDERS e lo mette negli asset.
"""

DESCRIPTION = ("Sfondi di menu e morte: l'ingresso della cripta e una tomba aperta, nello stile dei modelli "
               "del gioco.")
COMPARE = "docs/media/m17/prima/01_menu.jpg"

STYLE = ("stylized low poly 3D diorama render like the KayKit asset packs, isometric three quarter view from "
         "above, soft hand painted colors, simple shapes, dark fantasy, cinematic lighting, no text, no logo, "
         "no people, no characters")

# 1920x1088 è il multiplo di 16 più vicino al 1080 dello schermo: la finitura lo riporta a 1920x1080
SIZE = (1920, 1088)

IMAGES = {
    # i pulsanti stanno in una colonna a sinistra: la cripta a destra, la sinistra scura
    "menu_back": {
        "prompt": ("The stone entrance of an ancient crypt carved into a rocky hillside at night, a heavy open "
                   "doorway with faint warm torchlight inside, two burning torches, broken pillars and scattered "
                   "bones, deep blue moonlit night, the crypt on the right side of the picture, the left side "
                   "fading into empty dark shadow, " + STYLE),
        "seed": 1763,
        "size": SIZE,
        "finish": [("resize", (1920, 1080)), ("shade", 0.18, 0.5, 0.5, 0.75), ("darken", 0.15)],
    },
    # titolo e pulsante al centro
    "death_back": {
        # senza "3D render ... diorama" in testa il modello fa un'illustrazione piatta (prove del 17.6)
        "prompt": ("3D render of a low poly isometric diorama: an open stone grave inside a dark crypt, the "
                   "cracked lid pushed aside, a dented knight helmet and a broken sword lying on the floor "
                   "beside it, cobwebs, a cold pale blue light falling from above, dust in the air, gloomy and "
                   "quiet, " + STYLE),
        "seed": 1762,
        "size": SIZE,
        "finish": [("resize", (1920, 1080)), ("shade", 0.5, 0.5, 0.42, 0.7), ("darken", 0.2)],
    },
}

# i render del gioco: da dove vengono, dalla radice del repo, e la finitura
RENDERS = {
    # la finestra con le statistiche al centro
    "victory_back": {
        "source": "Recordings/Photos/victory_render.png",
        "finish": [("resize", (1920, 1080)), ("shade", 0.5, 0.5, 0.3, 0.5)],
    },
}
