"""La rovina (D9 della M13): un mucchietto di macerie con un rocchio di colonna caduto, un pezzo di
capitello, sassolini e un'asse spezzata."""
import math
import random

import bmesh

from meshlib import STONE, STONE_DARK, WOOD, box, finish, lathe, rock, transform

COMPARE = ("Dungeon/barrel_small.fbx", None)
PHOTO = {"target": (0.0, 0.0, 0.4), "ortho": 4.4}


def build_rubble(mat):
    rng = random.Random(47)
    bm = bmesh.new()
    # sassi grandi in mezzo, piccoli attorno, sassolini sparsi
    for k in range(16):
        a = rng.uniform(0, 2 * math.pi)
        if k < 4:
            d, s = rng.uniform(0.0, 0.5), rng.uniform(0.35, 0.55)
        elif k < 10:
            d, s = rng.uniform(0.5, 1.0), rng.uniform(0.15, 0.3)
        else:
            d, s = rng.uniform(0.7, 1.25), rng.uniform(0.07, 0.12)
        tile = STONE if rng.random() < 0.6 else STONE_DARK
        rock(bm, rng, (d * math.cos(a), d * math.sin(a), s * 0.35), (s, s * rng.uniform(0.7, 1.0), s * 0.7), tile)
    # il rocchio di colonna, disteso, con la scanalatura
    drum = lathe(bm, [(0.3, 0.0), (0.3, 0.55)], 12, lambda i, j: STONE, cap_top=STONE, cap_bottom=STONE)
    for ring in drum:
        for i, v in enumerate(ring):
            if i % 2:
                v.co.x *= 0.9
                v.co.y *= 0.9
    for v in [v for ring in drum for v in ring]:
        x, y, z = v.co
        v.co = (z + 0.35, y - 0.75, x + 0.3)
    # un pezzo di capitello: due lastre e un toro, inclinato contro il mucchio
    cap = box(bm, (0, 0, 0.06), (0.62, 0.62, 0.12), STONE)
    cap += box(bm, (0, 0, 0.17), (0.5, 0.5, 0.1), STONE_DARK)
    transform(cap, (-0.55, 0.65, 0.12), rot_z=0.4, rot_x=0.35)
    # un'asse spezzata
    plank = box(bm, (0, 0, 0), (0.9, 0.18, 0.06), WOOD)
    transform(plank, (0.3, 0.75, 0.12), rot_z=-0.5, rot_y=0.2)
    return finish(bm, "Rubble", mat, bevel=0.02)


MODELS = {"Rubble": build_rubble}
