"""Pozzo della vita e del mana (D7): bacino ottagonale di pietra con il liquido, quattro paracarri con la
punta d'oro, al centro un piedistallo con il cristallo. Il modello della prova del 19 ott 2026."""
import math

import bmesh

from meshlib import STEEL, STONE, STONE_DARK, GOLD, crystal, finish, prism, ring

COMPARE = ("Dungeon/chest.fbx", None)   # la cassa KayKit, per misura e colori
PHOTO = {"target": (0.0, 0.0, 0.7), "ortho": 6.0}

# (liquido, cristallo) per tipo
KINDS = {"Well_Life": ((3, 2), (2, 2)), "Well_Mana": ((2, 3), (6, 2))}


def build(name, mat):
    liquid, gem = KINDS[name]
    bm = bmesh.new()
    tw = math.pi / 8
    prism(bm, 8, 1.30, 1.22, 0.0, 0.14, STONE_DARK, tw)          # zoccolo
    ring(bm, 8, 1.08, 0.86, 0.14, 0.62, STONE, tw)               # parete del bacino
    ring(bm, 8, 1.16, 0.80, 0.62, 0.72, STEEL, tw)               # bordo
    prism(bm, 8, 0.86, 0.86, 0.40, 0.56, liquid, tw)             # il liquido, sotto il bordo
    for k in range(4):                                           # paracarri sugli spigoli
        a = tw + k * math.pi / 2
        x, y = 0.98 * math.cos(a), 0.98 * math.sin(a)
        prism(bm, 4, 0.13, 0.10, 0.72, 1.02, STONE, math.pi / 4 + a, x, y)
        prism(bm, 4, 0.07, 0.0, 1.02, 1.12, GOLD, math.pi / 4 + a, x, y)
    prism(bm, 6, 0.24, 0.18, 0.40, 0.95, STONE_DARK)             # piedistallo
    prism(bm, 6, 0.28, 0.28, 0.95, 1.02, STEEL)
    crystal(bm, 0.20, 1.02, 1.40, 1.95, gem)
    return finish(bm, name, mat, bevel=0.022)


MODELS = {name: (lambda mat, n=name: build(n, mat)) for name in KINDS}
