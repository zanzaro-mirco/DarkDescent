"""La terra smossa sulle tombe del borgo (D5 della M19): un cumulo basso e lungo quanto una fossa,
fatto di zolle scure, con qualche zolla e qualche sasso sparsi attorno. Sta davanti alla lapide rotta,
con il lato lungo lungo Y, e da lì escono gli scheletri dell'apertura (Skeletons_Awaken_Floor)."""
import random

import bmesh

from meshlib import STONE, WOOD, WOOD_DARK, finish, rock

COMPARE = ("Graveyard/grave_A.fbx", None)   # la tomba KayKit del cimitero, per misura e colori
PHOTO = {"target": (0.0, 0.0, 0.3), "ortho": 4.0}

LENGTH = 1.5
WIDTH = 0.8


def build_earth(mat):
    rng = random.Random(1903)
    bm = bmesh.new()
    # il cumulo: tante zolle sovrapposte dentro un'ellisse, più alte e più grandi verso il centro
    for _ in range(26):
        while True:
            x, y = rng.uniform(-1, 1), rng.uniform(-1, 1)
            if x * x + y * y <= 1:
                break
        fall = 1 - (x * x + y * y)
        s = 0.2 + 0.18 * fall
        rock(bm, rng, (x * WIDTH * 0.42, y * LENGTH * 0.45, 0.06 + 0.22 * fall), (s, s * rng.uniform(0.8, 1.2), s * 0.85),
             WOOD_DARK, jitter=0.35)
    # le zolle buttate fuori dalla fossa, più chiare perché asciutte
    for _ in range(9):
        side = rng.choice((-1, 1))
        s = rng.uniform(0.09, 0.16)
        rock(bm, rng, (side * rng.uniform(WIDTH * 0.5, WIDTH * 0.85), rng.uniform(-LENGTH * 0.55, LENGTH * 0.55), s * 0.3),
             (s, s * 0.9, s * 0.6), WOOD, jitter=0.35)
    # qualche sasso venuto su con la terra
    for _ in range(4):
        s = rng.uniform(0.06, 0.1)
        rock(bm, rng, (rng.uniform(-WIDTH * 0.7, WIDTH * 0.7), rng.uniform(-LENGTH * 0.6, LENGTH * 0.6), s * 0.3),
             (s, s, s * 0.7), STONE, jitter=0.3)
    return finish(bm, "DugEarth", mat, bevel=0.01)


MODELS = {"DugEarth": build_earth}
