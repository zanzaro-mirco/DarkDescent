"""Lo studio (D9 della M13): una pila di libri con i segnalibri e gli angoli d'ottone, un libro aperto,
una pergamena arrotolata, il calamaio con la penna e un mozzicone di candela."""
import math
import random

import bmesh

from meshlib import (BLACK, BLUE, FLAME, GOLD, LEAF, PAPER, RED, SKY, WHITE, WOOD, WOOD_DARK, beam, box,
                     finish, lathe, prism, transform)

COMPARE = ("Dungeon/box_small.fbx", None)
PHOTO = {"target": (0.0, 0.0, 0.35), "ortho": 3.4}

COVERS = [RED, BLUE, LEAF, WOOD_DARK, SKY, WOOD]


def book(bm, center, size, cover, rot_z=0.0, rot_x=0.0, corners=False, ribbon=None):
    """Un libro chiuso: copertina, pagine rientrate su tre lati, il dorso a -x, a scelta gli angoli
    d'ottone e un segnalibro che pende dal taglio."""
    w, d, h = size
    faces = box(bm, (0, 0, 0), (w, d, h), cover)
    faces += box(bm, (0.03, 0, 0), (w - 0.04, d - 0.06, h - 0.05), PAPER)
    faces += box(bm, (-w / 2 + 0.02, 0, 0), (0.05, d + 0.01, h + 0.01), cover)          # il dorso tondeggiante
    if corners:
        for sx in (-1, 1):
            for sy in (-1, 1):
                faces += box(bm, (sx * (w / 2 - 0.05), sy * (d / 2 - 0.05), 0), (0.1, 0.1, h + 0.012), GOLD)
    if ribbon is not None:
        faces += box(bm, (w / 2 - 0.01, -0.08, -h / 2 + 0.02), (0.06, 0.035, h * 0.4 + 0.08), ribbon)
    transform(faces, center, rot_z=rot_z, rot_x=rot_x)
    return faces


def build_books(mat):
    rng = random.Random(31)
    bm = bmesh.new()
    # la pila: libri sempre più piccoli, ognuno girato un po'
    z = 0.0
    sizes = [(0.66, 0.48, 0.13), (0.56, 0.42, 0.10), (0.52, 0.38, 0.13), (0.44, 0.34, 0.09)]
    for k, (w, d, h) in enumerate(sizes):
        book(bm, (-0.25, 0.05, z + h / 2), (w, d, h), COVERS[k], rot_z=rng.uniform(-0.35, 0.35),
             corners=k == 0, ribbon=RED if k == 2 else None)
        z += h
    # il mozzicone di candela in cima alla pila
    wax = lathe(bm, [(0.07, z), (0.075, z + 0.02), (0.065, z + 0.16), (0.05, z + 0.17)], 8, lambda i, j: WHITE,
                cap_top=WHITE)
    for v in [v for ring in wax for v in ring]:
        v.co.x -= 0.25
        v.co.y += 0.05
    prism(bm, 6, 0.028, 0.0, z + 0.17, z + 0.27, FLAME, cx=-0.25, cy=0.05)
    # due libri a terra, uno appoggiato storto alla pila
    book(bm, (0.42, -0.32, 0.055), (0.5, 0.38, 0.11), COVERS[4], rot_z=0.9, ribbon=GOLD)
    book(bm, (0.18, 0.42, 0.2), (0.48, 0.36, 0.1), COVERS[5], rot_z=-0.3, rot_x=0.9)
    # il libro aperto: due metà a V con le pagine sopra
    for side in (-1, 1):
        half = box(bm, (side * 0.17, 0, 0.0), (0.34, 0.44, 0.03), RED)
        half += box(bm, (side * 0.16, 0, 0.025), (0.31, 0.4, 0.035), PAPER)
        transform(half, (0, 0, 0), rot_y=-side * 0.18)
        transform(half, (0.45, 0.25, 0.06), rot_z=0.4)
    # il calamaio e la penna d'oca
    ink = lathe(bm, [(0.07, 0.0), (0.09, 0.03), (0.09, 0.09), (0.05, 0.12), (0.05, 0.14)], 8,
                lambda i, j: BLACK, cap_top=BLACK, cap_bottom=BLACK)
    for v in [v for ring in ink for v in ring]:
        v.co.x += 0.6
        v.co.y -= 0.05
    beam(bm, (0.6, -0.05, 0.1), (0.48, 0.12, 0.45), 0.025, WHITE)
    beam(bm, (0.52, 0.06, 0.32), (0.47, 0.13, 0.47), 0.06, WHITE)
    # la pergamena arrotolata, distesa a terra, con il nastro
    roll = lathe(bm, [(0.07, 0.0), (0.075, 0.02), (0.075, 0.58), (0.07, 0.6)], 8, lambda i, j: PAPER,
                 cap_top=PAPER, cap_bottom=PAPER)
    tie = lathe(bm, [(0.078, 0.27), (0.078, 0.33)], 8, lambda i, j: RED)
    for v in [v for ring in roll + tie for v in ring]:
        x, y, z = v.co
        x, y, z = z - 0.75, x - 0.45, y + 0.075
        a = 0.5
        v.co = (x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a), z)
    return finish(bm, "Books", mat, bevel=0.007)


MODELS = {"Books": build_books}
