"""Il barile rotto (D5 della M13): quello che resta del barile grande dei KayKit dopo il colpo. Il fondo
con le doghe in piedi spezzate ad altezze diverse e il primo cerchio, doghe e il cerchio di sopra a
terra, schegge sparse. Il pacchetto gratuito non ha i pezzi del barile. E il barile esplosivo (D8):
grande come quello dei KayKit, con i cerchi rossi, una targa di pericolo davanti e la miccia in cima."""
import math
import random

import bmesh

from meshlib import (BLACK, GOLD, PAPER, RED, STEEL, WOOD, WOOD_DARK, beam, box, finish, lathe, prism, rbox, ring, torus,
                     transform)

COMPARE = ("Dungeon/barrel_large.fbx", None)
PHOTO = {"target": (0.0, 0.0, 0.5), "ortho": 6.0}

STAVES = 12
RADIUS = 0.84


def standing_stave(bm, rng, i):
    """Una doga ancora in piedi, spezzata in cima: i due spigoli di sopra a altezze diverse."""
    width = 2 * math.pi * RADIUS / STAVES * 0.96
    height = rng.choice((0.3, 0.45, 0.6, 0.85, 1.05))
    faces = rbox(bm, (0, 0, height / 2), (0.09, width, height), WOOD)
    top = [v for v in {v for f in faces for v in f.verts} if v.co.z > height / 2]
    for v in top:
        v.co.z -= rng.uniform(0.0, 0.22) if v.co.y > 0 else rng.uniform(0.0, 0.08)
    a = 2 * math.pi * i / STAVES
    transform(faces, (RADIUS * math.cos(a), RADIUS * math.sin(a), 0.0), rot_z=a, rot_y=-0.05)


def lying_stave(bm, center, length, rot_z):
    """Una doga volata via, distesa a terra: lunga, stretta e appena curva."""
    faces = rbox(bm, (0, 0, 0), (length, 0.32, 0.09), WOOD)
    for v in {v for f in faces for v in f.verts}:
        v.co.z += 0.08 * (1 - (2 * v.co.x / length) ** 2)
    transform(faces, (center[0], center[1], 0.05), rot_z=rot_z)


def build_barrel_broken(mat):
    rng = random.Random(31)
    bm = bmesh.new()
    prism(bm, STAVES, 0.82, 0.82, 0.0, 0.08, WOOD_DARK)          # il fondo
    for i in range(STAVES):
        if i not in (2, 7):                                        # due doghe mancano del tutto
            standing_stave(bm, rng, i)
    ring(bm, STAVES, 0.93, 0.84, 0.12, 0.26, STEEL)               # il primo cerchio, ancora al suo posto
    # le doghe volate via, verso dove mancano
    lying_stave(bm, (1.35, 0.55, 0.0), 1.3, 0.5)
    lying_stave(bm, (-1.15, 1.05, 0.0), 1.15, -0.8)
    lying_stave(bm, (-1.3, -0.75, 0.0), 0.9, 2.2)
    # il cerchio di sopra, caduto di lato e un po' storto
    torus(bm, (0.35, -1.45, 0.05), 0.55, 0.045, STEEL, rot_x=0.06, sides=14, segments=4)
    # schegge
    for k in range(10):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(1.05, 1.6)
        chip = rbox(bm, (0, 0, 0), (rng.uniform(0.18, 0.32), 0.07, 0.05), WOOD)
        transform(chip, (d * math.cos(a), d * math.sin(a), 0.025), rot_z=rng.uniform(0, math.pi))
    return finish(bm, "Barrel_Broken", mat, bevel=0.012)


def build_barrel_explosive(mat):
    bm = bmesh.new()
    profile = [(0.78, 0.0), (0.87, 0.45), (0.92, 1.0), (0.87, 1.55), (0.78, 2.0)]
    lathe(bm, profile, STAVES, lambda i, j: WOOD, cap_top=WOOD_DARK, cap_bottom=WOOD_DARK)
    for z0, z1, r in ((0.16, 0.32, 0.86), (0.92, 1.08, 0.95), (1.68, 1.84, 0.86)):   # i cerchi rossi
        ring(bm, STAVES, r, r - 0.08, z0, z1, RED)
    # la targa di pericolo davanti (-Y): carta chiara con un rombo rosso
    box(bm, (0, -0.88, 1.36), (0.5, 0.06, 0.4), PAPER)
    mark = rbox(bm, (0, 0, 0), (0.22, 0.03, 0.22), RED)
    transform(mark, (0, -0.92, 1.36), rot_y=0.785)
    # la miccia: un tappo d'ottone sul coperchio e il cordone nero che si piega
    prism(bm, 6, 0.08, 0.07, 2.0, 2.08, GOLD, cx=0.3, cy=0.15)
    beam(bm, (0.3, 0.15, 2.06), (0.36, 0.2, 2.24), 0.04, BLACK)
    beam(bm, (0.36, 0.2, 2.24), (0.5, 0.26, 2.3), 0.04, BLACK)
    return finish(bm, "Barrel_Explosive", mat, bevel=0.012)


MODELS = {"Barrel_Broken": build_barrel_broken, "Barrel_Explosive": build_barrel_explosive}
PHOTOS = {"Barrel_Explosive": {"target": (0.0, 0.0, 1.2), "ortho": 6.0}}
