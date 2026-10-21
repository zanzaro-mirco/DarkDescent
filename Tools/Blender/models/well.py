"""Pozzo della vita e del mana (D7): bacino ottagonale di pietra, quattro paracarri con la punta d'oro, al
centro un piedistallo. Il liquido e il cristallo sono modelli a parte: in Unity il liquido si abbassa e il
cristallo si spegne quando il pozzo è usato, e il colore (rosso o blu) è quello del loro materiale. Il
modello della prova del 19 ott 2026, diviso in pezzi nel passo 13.6."""
import math

import bmesh

from meshlib import BLUE, STEEL, STONE, STONE_DARK, GOLD, crystal, finish, prism, ring

COMPARE = ("Dungeon/chest.fbx", None)   # la cassa KayKit, per misura e colori
PHOTO = {"target": (0.0, 0.0, 0.7), "ortho": 6.0}
PHOTOS = {"Well_Liquid": {"target": (0.0, 0.0, 0.3), "ortho": 4.0}, "Crystal": {"target": (0.0, 0.0, 0.5), "ortho": 4.0}}

TWIST = math.pi / 8


def build_well(mat):
    bm = bmesh.new()
    prism(bm, 8, 1.30, 1.22, 0.0, 0.14, STONE_DARK, TWIST)        # zoccolo, e il fondo del bacino
    ring(bm, 8, 1.08, 0.86, 0.14, 0.62, STONE, TWIST)             # parete del bacino
    ring(bm, 8, 1.16, 0.80, 0.62, 0.72, STEEL, TWIST)             # bordo
    for k in range(4):                                            # paracarri sugli spigoli
        a = TWIST + k * math.pi / 2
        x, y = 0.98 * math.cos(a), 0.98 * math.sin(a)
        prism(bm, 4, 0.13, 0.10, 0.72, 1.02, STONE, math.pi / 4 + a, x, y)
        prism(bm, 4, 0.07, 0.0, 1.02, 1.12, GOLD, math.pi / 4 + a, x, y)
    prism(bm, 6, 0.24, 0.18, 0.14, 0.95, STONE_DARK)              # piedistallo
    prism(bm, 6, 0.28, 0.28, 0.95, 1.02, STEEL)
    return finish(bm, "Well", mat, bevel=0.022)


def build_liquid(mat):
    """Il pelo del liquido: un ottagono sottile che sta dentro la parete del bacino."""
    bm = bmesh.new()
    prism(bm, 8, 0.86, 0.86, 0.0, 0.12, BLUE, TWIST)
    return finish(bm, "Well_Liquid", mat, bevel=0.0)


def build_crystal(mat):
    """Il cristallo del pozzo e del santuario, con la base a terra: il piedistallo lo regge."""
    bm = bmesh.new()
    crystal(bm, 0.20, 0.0, 0.38, 0.93, BLUE)
    return finish(bm, "Crystal", mat, bevel=0.0)


MODELS = {"Well": build_well, "Well_Liquid": build_liquid, "Crystal": build_crystal}
