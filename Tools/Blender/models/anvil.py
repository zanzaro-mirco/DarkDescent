"""L'incudine del fabbro del borgo (D18 della M18): un ceppo di legno cerchiato di ferro e sopra
l'incudine scura, con il corno verso +X. Il fabbro le sta davanti e ci batte il martello (Hammering),
che tiene nella mano destra: il manico lungo Z, con l'impugnatura all'origine."""
import math

import bmesh

from meshlib import STEEL, STONE_DARK, WOOD, WOOD_DARK, box, finish, prism, ring, transform

COMPARE = ("Dungeon/barrel_small.fbx", None)   # il barile piccolo KayKit, per misura e colori
PHOTO = {"target": (0.0, 0.0, 0.45), "ortho": 2.6}
PHOTOS = {"SmithHammer": {"target": (0.0, 0.0, 0.25), "ortho": 1.6}}

STUMP = 0.42


def build_anvil(mat):
    bm = bmesh.new()
    prism(bm, 8, 0.30, 0.27, 0.0, STUMP, WOOD, math.pi / 8)                    # il ceppo
    ring(bm, 8, 0.29, 0.25, STUMP * 0.62, STUMP * 0.74, WOOD_DARK, math.pi / 8)  # il cerchio di ferro
    box(bm, (0.0, 0.0, STUMP + 0.04), (0.34, 0.24, 0.08), STONE_DARK)          # il piede
    box(bm, (0.0, 0.0, STUMP + 0.14), (0.18, 0.13, 0.12), STONE_DARK)          # la vita
    box(bm, (-0.02, 0.0, STUMP + 0.25), (0.46, 0.20, 0.10), STEEL, (0.3, 1.0))  # il piano
    horn = prism(bm, 6, 0.085, 0.0, 0.0, 0.24, STEEL)                          # il corno, steso verso +X
    transform(horn, (0.21, 0.0, STUMP + 0.26), rot_y=math.pi / 2)
    return finish(bm, "Anvil", mat, bevel=0.012)


def build_hammer(mat):
    bm = bmesh.new()
    prism(bm, 6, 0.026, 0.022, 0.0, 0.40, WOOD)                    # il manico
    box(bm, (0.0, 0.0, 0.43), (0.18, 0.11, 0.11), STEEL, (0.3, 1.0))  # la testa
    return finish(bm, "SmithHammer", mat, bevel=0.008)


MODELS = {"Anvil": build_anvil, "SmithHammer": build_hammer}
