"""Gli sfondi a tutto schermo di menu, morte e vittoria (M17, D6): illustrazioni in stile diorama low poly,
come i modelli KayKit, scurite dove stanno i testi. Negli asset vanno con

    python Tools/ComfyUI/make.py menu_back death_back victory_back --out Assets/_Project/Art/Generated/Backgrounds
"""

DESCRIPTION = ("Sfondi di menu, morte e vittoria: l'ingresso della cripta, una tomba aperta, l'alba "
               "sull'accampamento, nello stile dei modelli del gioco.")
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
                   "cracked lid pushed aside, a dented knight helmet and a broken sword lying on the floor beside it, cobwebs, a cold pale blue light falling from "
                   "above, dust in the air, gloomy and quiet, " + STYLE),
        "seed": 1762,
        "size": SIZE,
        "finish": [("resize", (1920, 1080)), ("shade", 0.5, 0.5, 0.42, 0.7), ("darken", 0.2)],
    },
    # la finestra con le statistiche al centro
    "victory_back": {
        "prompt": ("3D render of a low poly isometric diorama: dawn over a small adventurers camp in a forest "
                   "clearing, canvas tents, a dying campfire with a thin line of smoke, a wooden palisade, the first golden sunlight rising over distant "
                   "mountains, warm hopeful morning mood, " + STYLE),
        "seed": 1763,
        "size": SIZE,
        "finish": [("resize", (1920, 1080)), ("shade", 0.5, 0.5, 0.36, 0.65), ("darken", 0.1)],
    },
}
