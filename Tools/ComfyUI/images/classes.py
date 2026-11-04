"""Le icone delle classi per la creazione del personaggio (M21, D13): un emblema per il Guerriero e uno per il Mago,
nello stile delle icone dei talenti. Stanno sulla scheda di ogni classe. Per rifarle:

    python Tools/ComfyUI/make.py --takes 1 --out Assets/_Project/Art/Icons/Classes classes
"""

DESCRIPTION = "Gli emblemi delle classi per la schermata di creazione: il Guerriero e il Mago."
COMPARE = "Recordings/Photos/creazione/creation_2_mage.jpg"

STYLE = ("game class emblem icon in the style of Diablo III, hand painted digital illustration, dark fantasy, dramatic "
         "lighting, high contrast, rich colors with one glowing focal point, a single centered subject that fills "
         "the square frame, dark moody background with a soft vignette, no text, no letters, no border, no frame")

_FINISH = [("resize", 256)]


def _icon(subject, seed):
    return {"prompt": subject + ", " + STYLE, "seed": seed, "size": (1024, 1024), "finish": _FINISH}


_ICONS = (
    ("class_warrior", "a steel knight helmet with a visor and a red plume in front of two crossed swords, a heroic warrior emblem", 2702),
    ("class_mage", "a pointed purple wizard hat with a glowing blue crystal staff, floating magical sparks, a mage emblem", 2704),
)

IMAGES = {name: _icon(subject, seed) for name, subject, seed in _ICONS}
