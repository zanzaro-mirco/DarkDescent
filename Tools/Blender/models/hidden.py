"""I segreti delle stanze (25.2 della M25): la porta sigillata di pietra con le sue rune (un modello a parte, che
in Unity si accende), la leva col suo piedistallo e la sua asta (che gira attorno all'origine), il leggio col libro
aperto, la lapide con le incisioni e le catene del baule maledetto, fatte per stare sul forziere dei KayKit
(`chest.fbx`, 1,7 x 1,45 x 1,3 m). Il davanti è -Y, come nei KayKit: in Unity guarda la stanza."""
import math

import bmesh

from meshlib import (BLACK, PAPER, RED, STEEL, STONE, STONE_DARK, VIOLET, WOOD, WOOD_DARK, WOOD_LIGHT, box,
                     beam, crystal, finish, prism, rbox, transform)

COMPARE = ("Dungeon/wall_doorway.fbx", None)
COMPARES = {
    "Lever": ("Dungeon/pillar.fbx", None),
    "Lever_Handle": ("Dungeon/keyring.fbx", None),
    "Lectern": ("Dungeon/table_small_decorated_A.fbx", None),
    "Tablet": ("Dungeon/pillar.fbx", None),
    "ChestCurse": ("Dungeon/chest.fbx", None),
}
PHOTO = {"target": (0.0, 0.0, 2.0), "ortho": 11.0}
PHOTOS = {
    "SealedDoor_Runes": {"target": (0.0, 0.0, 2.0), "ortho": 11.0},
    "Lever": {"target": (0.0, 0.0, 0.9), "ortho": 4.0},
    "Lever_Handle": {"target": (0.0, 0.0, 0.5), "ortho": 3.0},
    "Lectern": {"target": (0.0, 0.0, 0.8), "ortho": 3.6},
    "Tablet": {"target": (0.0, 0.0, 1.0), "ortho": 4.0},
    "ChestCurse": {"target": (0.0, 0.0, 0.8), "ortho": 4.4},
}

DOOR_WIDTH = 4.0   # la cella dei livelli: la porta chiude un passaggio di una cella
DOOR_HEIGHT = 4.0
DOOR_DEPTH = 0.6


def build_door(mat):
    """La porta sigillata: i due stipiti e l'architrave di pietra scura, i due battenti chiusi al centro con una
    specchiatura sopra l'altra, le bandelle di ferro e il fermo di mezzo. Le rune sono un modello a parte."""
    bm = bmesh.new()
    half = DOOR_WIDTH / 2
    # lo stipite e la soglia
    for sx in (-1, 1):
        box(bm, (sx * (half - 0.25), 0, DOOR_HEIGHT / 2), (0.5, DOOR_DEPTH, DOOR_HEIGHT), STONE_DARK)
    box(bm, (0, 0, DOOR_HEIGHT - 0.25), (DOOR_WIDTH, DOOR_DEPTH, 0.5), STONE_DARK)
    box(bm, (0, 0, 0.06), (DOOR_WIDTH - 1.0, DOOR_DEPTH + 0.12, 0.12), STONE)
    # la chiave di volta sopra
    box(bm, (0, -DOOR_DEPTH / 2 - 0.03, DOOR_HEIGHT - 0.25), (0.5, 0.08, 0.56), STONE)
    # i due battenti: uno spesso lastrone con la specchiatura in rilievo
    leaf_h = DOOR_HEIGHT - 0.5 - 0.12
    for sx in (-1, 1):
        box(bm, (sx * 0.74, 0.04, 0.12 + leaf_h / 2), (1.46, 0.4, leaf_h), STONE)
        for z in (1.0, 2.7):
            box(bm, (sx * 0.74, -0.18, z), (1.0, 0.05, 1.3), STONE, (0.25, 0.9))
        for z in (0.75, 3.0):
            box(bm, (sx * 0.74, -0.22, z), (1.4, 0.04, 0.12), STEEL)           # le bandelle
            for bx in (-0.5, 0.5):
                prism(bm, 6, 0.04, 0.03, z - 0.04, z + 0.05, BLACK, cx=sx * 0.74 + bx, cy=-0.24)
    # il fermo di mezzo, una sbarra in orizzontale con la serratura
    box(bm, (0, -0.25, 1.85), (1.1, 0.06, 0.16), STEEL)
    box(bm, (0, -0.28, 1.85), (0.26, 0.08, 0.3), BLACK)
    # la fessura dove si incontrano i battenti, scura
    box(bm, (0, -0.205, 0.12 + leaf_h / 2), (0.04, 0.02, leaf_h), BLACK)
    return finish(bm, "SealedDoor", mat, bevel=0.018)


def build_runes(mat):
    """Le rune che si accendono: due rombi sopra e sotto il fermo su ogni battente e un filo sull'architrave.
    Stanno a un pelo dalla porta (-Y), così il modello si sovrappone a quello della porta."""
    bm = bmesh.new()
    y = -0.3
    for sx in (-1, 1):
        for z in (1.0, 2.7):
            for k in range(2):
                bar = box(bm, (0, 0, 0), (0.7, 0.025, 0.06), VIOLET)
                transform(bar, (sx * 0.74, y, z), rot_y=0.785 if k == 0 else -0.785)
            box(bm, (sx * 0.74, y, z), (0.08, 0.03, 0.08), VIOLET)
    box(bm, (0, y + 0.02, 3.6), (1.4, 0.02, 0.06), VIOLET)
    crystal(bm, 0.08, 3.5, 3.6, 3.7, VIOLET, cx=0.0, cy=-0.32)
    # un pezzetto nascosto nella soglia della porta: finish() appoggia il modello a terra, e senza questo le rune scenderebbero
    box(bm, (0, 0.2, 0.01), (0.02, 0.02, 0.02), VIOLET)
    return finish(bm, "SealedDoor_Runes", mat, bevel=0.0)


def build_lever(mat):
    """Il piedistallo della leva: un blocco di pietra con la placca in alto e l'arco graduato dove l'asta corre.
    L'asta è un modello a parte, che gira attorno al suo piede."""
    bm = bmesh.new()
    box(bm, (0, 0, 0.08), (1.0, 0.8, 0.16), STONE_DARK)
    box(bm, (0, 0, 0.55), (0.76, 0.5, 0.78), STONE)
    box(bm, (0, 0, 0.98), (0.84, 0.58, 0.1), STONE_DARK)
    # la placca davanti con l'arco graduato in ferro
    box(bm, (0, -0.265, 0.62), (0.6, 0.03, 0.5), STONE_DARK, (0.3, 0.9))
    for k in range(5):
        a = -0.9 + k * 0.45
        tick = box(bm, (0, 0, 0), (0.04, 0.03, 0.1), STEEL)
        transform(tick, (math.sin(a) * 0.2, -0.285, 0.5 + math.cos(a) * 0.2), rot_y=a)
    # il perno in cima, dove si innesta l'asta (l'origine dell'asta va qui: z 1,05)
    box(bm, (0, 0, 1.05), (0.2, 0.3, 0.06), BLACK)
    return finish(bm, "Lever", mat, bevel=0.015)


def build_handle(mat):
    """L'asta: dal piede (l'origine, attorno a cui gira) fino al pomello. Dritta verso l'alto."""
    bm = bmesh.new()
    box(bm, (0, 0, 0.05), (0.12, 0.16, 0.1), BLACK)
    box(bm, (0, 0, 0.55), (0.07, 0.07, 0.9), STEEL)
    for z in (0.3, 0.6):
        box(bm, (0, 0, z), (0.1, 0.1, 0.05), STONE_DARK)
    prism(bm, 8, 0.0, 0.1, 0.98, 1.05, RED)
    prism(bm, 8, 0.1, 0.0, 1.05, 1.14, RED)
    return finish(bm, "Lever_Handle", mat, bevel=0.01)


def build_lectern(mat):
    """Il leggio: un fusto di legno su una base larga, il piano inclinato verso chi legge con il listello davanti, e
    sopra un grosso libro aperto con il segnalibro rosso e un mozzicone di candela."""
    bm = bmesh.new()
    box(bm, (0, 0, 0.07), (0.86, 0.72, 0.14), WOOD_DARK)
    box(bm, (0, 0, 0.1), (0.62, 0.5, 0.2), WOOD)
    box(bm, (0, 0, 0.62), (0.3, 0.3, 1.0), WOOD)
    box(bm, (0, 0, 1.1), (0.4, 0.4, 0.14), WOOD_DARK)
    # il piano e il listello
    slope = 0.45
    rbox(bm, (0, 0, 1.28), (0.98, 0.72, 0.07), WOOD_LIGHT, rot_x=slope)
    rbox(bm, (0, -0.33, 1.14), (0.98, 0.06, 0.1), WOOD_DARK, rot_x=slope)
    # il libro aperto: due metà a V sopra il piano
    for side in (-1, 1):
        half = box(bm, (side * 0.19, 0, 0.0), (0.37, 0.5, 0.04), RED)
        half += box(bm, (side * 0.18, 0, 0.035), (0.33, 0.46, 0.04), PAPER)
        transform(half, (0, 0, 0), rot_y=-side * 0.12)
        transform(half, (0, -0.05, 1.37), rot_x=slope)
    box(bm, (0.12, -0.3, 1.4), (0.03, 0.4, 0.015), RED)
    return finish(bm, "Lectern", mat, bevel=0.015)


def build_tablet(mat):
    """La lapide con le incisioni: una lastra alta su due gradini, righe di segni scavati e in cima un teschietto
    in rilievo; è dove il testo sta scolpito sul muro, in piedi nella stanza."""
    bm = bmesh.new()
    box(bm, (0, 0, 0.09), (1.2, 0.6, 0.18), STONE_DARK)
    box(bm, (0, 0, 0.26), (1.0, 0.44, 0.16), STONE)
    box(bm, (0, 0, 0.95), (0.84, 0.26, 1.24), STONE)
    box(bm, (0, 0, 1.62), (0.92, 0.3, 0.12), STONE_DARK)
    # le righe incise
    widths = (0.6, 0.56, 0.62, 0.5, 0.58)
    for k, w in enumerate(widths):
        box(bm, (0, -0.135, 0.62 + k * 0.17), (w, 0.02, 0.035), BLACK)
    # il sole inciso in cima: un cerchio e un punto, scavati
    prism(bm, 12, 0.12, 0.12, 1.28, 1.31, BLACK, cy=-0.13)
    prism(bm, 12, 0.085, 0.085, 1.285, 1.315, STONE, cy=-0.135)
    prism(bm, 6, 0.03, 0.03, 1.29, 1.32, BLACK, cy=-0.14)
    return finish(bm, "Tablet", mat, bevel=0.015)


def build_curse(mat):
    """Le catene del baule maledetto, fatte per stare sul forziere dei KayKit (1,7 x 1,45 x 1,3, il coperchio a
    volta con le sue due fasce): due catene che lo cingono da un lato all'altro seguendo la volta, un teschio
    sul davanti con due occhi viola e un cristallo viola in cima. Si mette sul forziere, stesso centro e stessa
    base; in Unity il forziere prende un materiale più scuro."""
    bm = bmesh.new()

    def arc_z(x):
        return 0.74 + 0.5 * (1.0 - (x / 0.86) ** 2) - 0.02

    for y in (-0.32, 0.32):
        xs = [-0.88 + 0.22 * k for k in range(9)]
        pts = [(x, y, arc_z(x)) for x in xs]
        pts = [(-0.9, y, 0.45)] + pts + [(0.9, y, 0.45)]
        for k in range(len(pts) - 1):
            beam(bm, pts[k], pts[k + 1], 0.06, STEEL)
        for k, p in enumerate(pts):
            box(bm, p, (0.1, 0.1, 0.1), BLACK if k % 2 == 0 else STEEL)
    # il teschio sul davanti, sopra il fermo
    box(bm, (0, -0.78, 0.86), (0.34, 0.05, 0.38), STEEL)
    prism(bm, 8, 0.15, 0.15, 0.86, 1.02, STONE_DARK, cy=-0.84)
    box(bm, (0, -0.95, 0.84), (0.2, 0.06, 0.1), STONE_DARK)
    for sx in (-1, 1):
        box(bm, (sx * 0.06, -0.98, 0.94), (0.06, 0.04, 0.06), VIOLET)
    # il cristallo viola in cima alla volta
    crystal(bm, 0.12, 1.2, 1.32, 1.52, VIOLET, cx=0.0, cy=0.0)
    # un pezzetto nascosto dentro il forziere: finish() appoggia il modello a terra e le catene non devono scendere
    box(bm, (0, 0, 0.01), (0.02, 0.02, 0.02), STEEL)
    return finish(bm, "ChestCurse", mat, bevel=0.012)


MODELS = {
    "SealedDoor": build_door,
    "SealedDoor_Runes": build_runes,
    "Lever": build_lever,
    "Lever_Handle": build_handle,
    "Lectern": build_lectern,
    "Tablet": build_tablet,
    "ChestCurse": build_curse,
}
