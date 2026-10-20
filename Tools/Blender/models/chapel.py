"""La cappella, il quinto tema (D9 della M13): il sarcofago con il coperchio appena scostato, la statua
del guardiano e l'altare con il candelabro e il libro. Il davanti è -Y, come nei KayKit: in Unity
guarda la stanza."""
import math

import bmesh

from meshlib import (BLACK, CRIMSON, FLAME, GOLD, PAPER, RED, STONE, STONE_DARK, WHITE, WOOD, beam, box,
                     finish, lathe, prism, rbox, torus, transform)

COMPARE = ("Dungeon/chest.fbx", None)
COMPARES = {"Statue": ("Dungeon/pillar.fbx", None), "Altar": ("Dungeon/table_medium_tablecloth.fbx", None)}
PHOTO = {"target": (0.0, 0.0, 0.6), "ortho": 6.5}
PHOTOS = {"Statue": {"target": (0.0, 0.0, 1.7), "ortho": 6.0}, "Altar": {"target": (0.0, 0.0, 0.8), "ortho": 6.0}}


def candle(bm, x, y, base, height, radius=0.06):
    wax = lathe(bm, [(radius, base), (radius, base + height), (radius * 0.7, base + height + 0.02)], 8,
                lambda i, j: WHITE, cap_top=WHITE)
    for v in [v for ring in wax for v in ring]:
        v.co.x += x
        v.co.y += y
    prism(bm, 6, radius * 0.45, 0.0, base + height + 0.02, base + height + 0.13, FLAME, cx=x, cy=y)


def build_sarcophagus(mat):
    bm = bmesh.new()
    for sx in (-1, 1):                                                 # quattro piedi
        for sy in (-1, 1):
            box(bm, (sx * 1.15, sy * 0.48, 0.08), (0.36, 0.36, 0.16), STONE_DARK)
    box(bm, (0, 0, 0.2), (2.6, 1.2, 0.1), STONE_DARK)                  # zoccolo
    box(bm, (0, 0, 0.6), (2.45, 1.08, 0.7), STONE, (0.2, 0.9))         # cassa
    for sx in (-1, 1):                                                 # lesene agli angoli
        for sy in (-1, 1):
            box(bm, (sx * 1.2, sy * 0.52, 0.6), (0.14, 0.14, 0.72), STONE_DARK)
    for x in (-0.62, 0.62):                                            # due specchiature davanti
        box(bm, (x, -0.555, 0.58), (0.95, 0.04, 0.42), STONE, (0.35, 1.0))
        box(bm, (x, -0.565, 0.58), (0.8, 0.03, 0.3), STONE_DARK)
    box(bm, (0, -0.56, 0.88), (2.2, 0.03, 0.08), STONE_DARK)           # la fascia dell'iscrizione
    for k in range(9):
        box(bm, (-0.8 + 0.2 * k, -0.58, 0.88), (0.08, 0.02, 0.04), GOLD)
    # il coperchio, scostato di un palmo: si vede il buio dentro
    box(bm, (-1.1, 0, 0.94), (0.2, 0.95, 0.02), BLACK)
    lid = box(bm, (0, 0, 1.02), (2.6, 1.22, 0.14), STONE_DARK)
    for side in (-1, 1):                                               # due falde fino al colmo
        top = rbox(bm, (0, 0, 0), (2.5, 0.62, 0.1), STONE, rot_x=side * 0.2)
        transform(top, (0, side * 0.29, 1.15))
        lid += top
    lid += box(bm, (0, 0, 1.23), (2.5, 0.14, 0.08), STONE_DARK)        # colmo
    lid += box(bm, (0.1, 0, 1.285), (1.3, 0.12, 0.04), GOLD, (0.4, 1.0))  # la croce stesa sul colmo
    lid += box(bm, (-0.25, 0, 1.285), (0.12, 0.5, 0.04), GOLD, (0.4, 1.0))
    transform(lid, (0.22, 0.04, 0), rot_z=0.05)
    return finish(bm, "Sarcophagus", mat, bevel=0.022)


def build_statue(mat):
    bm = bmesh.new()
    box(bm, (0, 0, 0.25), (1.4, 1.4, 0.5), STONE_DARK)                 # basamento
    box(bm, (0, 0, 0.6), (1.15, 1.15, 0.2), STONE)
    box(bm, (0, -0.71, 0.27), (0.6, 0.03, 0.2), GOLD)                  # la targa
    # la veste con le pieghe: raggio alternato tra un lato e l'altro
    robe = lathe(bm, [(0.54, 0.7), (0.48, 1.0), (0.39, 1.6), (0.36, 2.0)], 16, lambda i, j: STONE, cap_bottom=STONE)
    for ring in robe[:-1]:
        for i, v in enumerate(ring):
            if i % 2:
                v.co.x *= 0.9
                v.co.y *= 0.9
    torus(bm, (0, 0, 1.45), 0.4, 0.045, WOOD, sides=16, segments=5)   # la cintura di corda
    beam(bm, (0.15, -0.36, 1.43), (0.2, -0.43, 1.05), 0.06, WOOD)
    # le spalle con il mantello, il collo, la testa
    lathe(bm, [(0.36, 2.0), (0.5, 2.1), (0.46, 2.24), (0.16, 2.3), (0.14, 2.38)], 16, lambda i, j: STONE)
    for side in (-1, 1):                                               # spallacci di pietra
        pad = lathe(bm, [(0.2, 0.0), (0.19, 0.08), (0.12, 0.16), (0.0, 0.18)], 10, lambda i, j: STONE_DARK)
        for v in [v for ring in pad for v in ring]:
            v.co.x += side * 0.38
            v.co.z += 2.12
    lathe(bm, [(0.2, 2.36), (0.26, 2.5), (0.26, 2.7), (0.2, 2.86), (0.09, 2.95), (0.0, 2.97)], 12,
          lambda i, j: STONE)
    hood = lathe(bm, [(0.3, 2.4), (0.31, 2.62), (0.27, 2.86), (0.15, 3.0), (0.0, 3.04)], 12, lambda i, j: STONE_DARK)
    for ring in hood:
        for v in ring:
            if v.co.y < -0.15:
                v.co.y = -0.15 + (v.co.y + 0.15) * 0.3
    box(bm, (0, -0.22, 2.62), (0.24, 0.02, 0.05), BLACK)                # gli occhi nell'ombra
    # la spada piantata davanti, le mani sul pomo
    box(bm, (0, -0.52, 1.3), (0.14, 0.06, 1.1), STONE)                 # lama
    box(bm, (0, -0.52, 1.3), (0.04, 0.07, 1.0), STONE_DARK)            # sguscio
    box(bm, (0, -0.52, 1.88), (0.6, 0.12, 0.1), STONE_DARK)            # guardia
    box(bm, (0, -0.52, 2.02), (0.09, 0.09, 0.2), STONE_DARK)           # impugnatura
    box(bm, (0, -0.52, 2.15), (0.3, 0.2, 0.13), STONE)                 # le mani
    for side in (-1, 1):                                               # braccio e avambraccio
        elbow = (side * 0.42, -0.3, 1.85)
        beam(bm, (side * 0.4, -0.05, 2.12), elbow, 0.17, STONE)
        beam(bm, elbow, (side * 0.1, -0.5, 2.15), 0.15, STONE)
    return finish(bm, "Statue", mat, bevel=0.018)


def build_altar(mat):
    bm = bmesh.new()
    box(bm, (0, 0, 0.1), (2.3, 1.3, 0.2), STONE_DARK)                  # gradino
    box(bm, (0, 0, 0.62), (1.9, 0.95, 0.84), STONE, (0.2, 0.9))        # blocco
    for sx in (-1, 1):                                                 # colonnine agli angoli davanti
        column = lathe(bm, [(0.1, 0.2), (0.08, 0.3), (0.08, 0.95), (0.11, 1.03)], 8, lambda i, j: STONE_DARK)
        for v in [v for ring in column for v in ring]:
            v.co.x += sx * 0.88
            v.co.y -= 0.42
    box(bm, (0, 0, 1.1), (2.1, 1.1, 0.14), STONE_DARK)                 # piano
    box(bm, (0, 0.02, 1.18), (0.7, 1.12, 0.02), CRIMSON)               # il drappo sul piano
    box(bm, (0, -0.56, 0.86), (0.7, 0.025, 0.62), CRIMSON, (0.0, 0.8)) # che scende davanti
    box(bm, (0, -0.575, 0.57), (0.7, 0.02, 0.05), GOLD)                # orlo d'oro
    torus(bm, (0, -0.585, 0.9), 0.14, 0.025, GOLD, rot_x=math.pi / 2, sides=12, segments=4)   # il simbolo
    # il candelabro a tre bracci, a sinistra
    stand = lathe(bm, [(0.14, 1.17), (0.1, 1.2), (0.04, 1.25), (0.04, 1.55)], 8, lambda i, j: GOLD, cap_bottom=GOLD)
    for v in [v for ring in stand for v in ring]:
        v.co.x -= 0.65
    box(bm, (-0.65, 0, 1.55), (0.56, 0.06, 0.05), GOLD)
    for x in (-0.91, -0.65, -0.39):
        candle(bm, x, 0.0, 1.57, 0.22 if x == -0.65 else 0.16, radius=0.045)
    # il libro aperto e due candele sciolte, a destra
    for side in (-1, 1):
        half = box(bm, (side * 0.15, 0, 0.0), (0.3, 0.4, 0.03), RED)
        half += box(bm, (side * 0.14, 0, 0.025), (0.27, 0.36, 0.035), PAPER)
        transform(half, (0, 0, 0), rot_y=-side * 0.15)
        transform(half, (0.45, -0.05, 1.21), rot_z=-0.2)
    candle(bm, 0.82, 0.25, 1.17, 0.3)
    candle(bm, 0.95, 0.05, 1.17, 0.18)
    return finish(bm, "Altar", mat, bevel=0.012)


MODELS = {"Sarcophagus": build_sarcophagus, "Statue": build_statue, "Altar": build_altar}
