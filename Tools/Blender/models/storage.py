"""Il deposito (D9 della M13): il sacco di iuta legato e rattoppato, l'anfora di terracotta e l'anfora
rotta con i cocci a terra."""
import math
import random

import bmesh

from meshlib import (BLACK, BURLAP, OCHRE, SLATE, TERRACOTTA, WOOD, beam, finish, jagged_lathe, lathe, paint,
                     rbox, rock, torus, transform)

COMPARE = ("Dungeon/barrel_small.fbx", None)
PHOTO = {"target": (0.0, 0.0, 0.75), "ortho": 4.0}
PHOTOS = {"Amphora_Broken": {"target": (0.0, 0.0, 0.4), "ortho": 4.6}}


def build_sack(mat):
    rng = random.Random(11)
    bm = bmesh.new()
    profile = [(0.30, 0.0), (0.44, 0.07), (0.50, 0.28), (0.51, 0.55), (0.46, 0.78), (0.32, 0.93),
               (0.15, 1.0), (0.12, 1.06), (0.22, 1.13), (0.17, 1.2), (0.05, 1.19)]
    rings = lathe(bm, profile, 12, lambda i, j: BURLAP)
    # un sacco non è un vaso: i fianchi bitorzoluti, la pancia che si appoggia, la cima arruffata
    for j, ring in enumerate(rings[1:-1], start=1):
        for v in ring:
            k = 1 + rng.uniform(-0.06, 0.06)
            v.co.x *= k
            v.co.y *= k * (1.06 if j < 6 else 1.0)
            if j >= 8:
                v.co.z += rng.uniform(-0.05, 0.05)
    # la corda: il giro e i due capi che pendono
    torus(bm, (0, 0, 1.04), 0.14, 0.035, WOOD, sides=10, segments=5)
    beam(bm, (0.12, -0.06, 1.04), (0.2, -0.16, 0.86), 0.05, WOOD)
    beam(bm, (0.1, -0.1, 1.03), (0.12, -0.24, 0.9), 0.05, WOOD)
    # una toppa di stoffa diversa, cucita sul fianco davanti: tangente alla pancia
    a = -1.95
    patch = rbox(bm, (0, 0, 0), (0.26, 0.03, 0.22), SLATE)
    transform(patch, (0.5 * math.cos(a), 0.5 * math.sin(a), 0.5), rot_z=a + math.pi / 2)
    for dz in (-0.08, 0.08):
        stitch = rbox(bm, (0, 0, 0), (0.2, 0.02, 0.02), WOOD)
        transform(stitch, (0.515 * math.cos(a), 0.515 * math.sin(a), 0.5 + dz), rot_z=a + math.pi / 2)
    return finish(bm, "Sack", mat, bevel=0.012)


AMPHORA = [(0.16, 0.0), (0.21, 0.03), (0.19, 0.08), (0.30, 0.16), (0.42, 0.34), (0.47, 0.58), (0.465, 0.66),
           (0.455, 0.74), (0.44, 0.92), (0.30, 1.12), (0.18, 1.22), (0.165, 1.27), (0.15, 1.42), (0.23, 1.47),
           (0.23, 1.52), (0.19, 1.54)]


def build_amphora(mat):
    bm = bmesh.new()

    def tile(i, j):
        if j == 5:
            return BLACK
        if j == 10:
            return OCHRE
        return TERRACOTTA

    lathe(bm, AMPHORA, 16, tile, cap_top=BLACK, cap_bottom=TERRACOTTA)
    # le anse: dal collo alla spalla, a gomito
    for side in (-1, 1):
        beam(bm, (side * 0.16, 0, 1.38), (side * 0.36, 0, 1.36), 0.06, TERRACOTTA)
        beam(bm, (side * 0.36, 0, 1.36), (side * 0.38, 0, 1.08), 0.06, TERRACOTTA)
    return finish(bm, "Amphora", mat, bevel=0.01)


def shard(bm, rng, center, radius, span, height, tile):
    """Un coccio curvo: un pezzo di guscio di un vaso, disteso a terra sul fianco."""
    a0 = rng.uniform(0, 2 * math.pi)
    steps = 3
    outer, inner = [], []
    for i in range(steps + 1):
        a = a0 + span * i / steps
        for r, layer in ((radius, outer), (radius - 0.035, inner)):
            layer.append([bm.verts.new((r * math.cos(a), r * math.sin(a), z)) for z in (0.0, height)])
    faces = []
    for i in range(steps):
        faces.append(bm.faces.new((outer[i][0], outer[i + 1][0], outer[i + 1][1], outer[i][1])))
        faces.append(bm.faces.new((inner[i][1], inner[i + 1][1], inner[i + 1][0], inner[i][0])))
        faces.append(bm.faces.new((outer[i][1], outer[i + 1][1], inner[i + 1][1], inner[i][1])))
        faces.append(bm.faces.new((inner[i][0], inner[i + 1][0], outer[i + 1][0], outer[i][0])))
    faces.append(bm.faces.new((outer[0][0], outer[0][1], inner[0][1], inner[0][0])))
    faces.append(bm.faces.new((inner[-1][0], inner[-1][1], outer[-1][1], outer[-1][0])))
    paint(bm, faces, tile, 0.0, height)
    mid = a0 + span / 2
    transform(faces, (0, 0, 0), rot_z=-mid)
    transform(faces, (-radius, 0, 0))
    transform(faces, (0, 0, 0), rot_y=math.pi / 2 - 0.25)
    transform(faces, center, rot_z=rng.uniform(0, 2 * math.pi))


def build_amphora_broken(mat):
    rng = random.Random(23)
    bm = bmesh.new()
    lower = [(r, z) for r, z in AMPHORA if z <= 0.6]
    body = jagged_lathe(bm, lower, 16, TERRACOTTA, rng, rim_drop=0.28, cap_bottom=TERRACOTTA)
    bmesh.ops.solidify(bm, geom=body, thickness=0.04)
    # i cocci: pezzi curvi grandi vicino, schegge piatte più in là
    for k in range(4):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0.65, 0.9)
        shard(bm, rng, (d * math.cos(a), d * math.sin(a), 0.04), 0.46, rng.uniform(0.5, 0.8),
              rng.uniform(0.22, 0.32), TERRACOTTA)
    for k in range(6):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0.8, 1.2)
        w = rng.uniform(0.12, 0.22)
        rock(bm, rng, (d * math.cos(a), d * math.sin(a), 0.025), (w, w * rng.uniform(0.5, 0.9), 0.04), TERRACOTTA,
             jitter=0.4)
    # il collo, intero, rotolato più in là
    neck = lathe(bm, [(0.16, 0.0), (0.14, 0.17), (0.23, 0.22), (0.23, 0.27), (0.19, 0.29)], 12,
                 lambda i, j: TERRACOTTA, cap_top=BLACK)
    for v in [v for ring in neck for v in ring]:
        x, y, z = v.co
        v.co = (z - 0.95, y - 0.55, x + 0.23)
    return finish(bm, "Amphora_Broken", mat, bevel=0.01)


MODELS = {"Sack": build_sack, "Amphora": build_amphora, "Amphora_Broken": build_amphora_broken}
