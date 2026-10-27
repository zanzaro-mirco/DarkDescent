"""Il buio della grotta del borgo e del cunicolo della cripta (D6 e D7 della M18): una galleria con la
volta ad arco che entra di qualche metro, nera dentro, con il davanti verso -Y. I massi attorno non sono
qui: sono i sassi di Forest Nature, messi nei prefab, che coprono il bordo dell'arco. Dalla volta della
grotta pendono radici, perché sta all'aperto sotto la collina; il cunicolo no."""
import math
import random

import bmesh

from meshlib import BLACK, WOOD_DARK, beam, finish, paint

COMPARE = ("Dungeon/wall.fbx", None)
PHOTO = {"target": (2.6, 0.0, 1.8), "ortho": 9.0}
PHOTOS = {"TunnelHole": {"target": (2.2, 0.0, 1.4), "ortho": 8.0}}


def gallery(bm, width, height, depth, sides=9):
    """Il profilo è un mezzo ovale su due pareti dritte; si estrude verso +Y e si chiude in fondo. Le
    facce guardano dentro: da fuori si vede il nero attraverso l'arco."""
    wall = height * 0.35
    profile = [(-width / 2, 0.0), (-width / 2, wall)]
    for k in range(1, sides):
        t = math.pi - math.pi * k / sides
        profile.append((math.cos(t) * width / 2, wall + math.sin(t) * (height - wall)))
    profile += [(width / 2, wall), (width / 2, 0.0)]
    front = [bm.verts.new((x, 0.0, z)) for x, z in profile]
    back = [bm.verts.new((x * 0.8, depth, z * 0.85)) for x, z in profile]
    faces = []
    for i in range(len(profile) - 1):
        faces.append(bm.faces.new((front[i], back[i], back[i + 1], front[i + 1])))
    faces.append(bm.faces.new(back))
    floor = bm.faces.new((front[0], front[-1], back[-1], back[0]))
    faces.append(floor)
    paint(bm, faces, BLACK, 0.0, height)
    return faces


def roots(bm, rng, width, height, count):
    """Radici che pendono dall'arco, a due tratti storti, appena dentro."""
    for _ in range(count):
        x = rng.uniform(-width * 0.35, width * 0.35)
        top = (x, 0.25, height * (1 - (2 * x / width) ** 2) ** 0.5 * 0.95)
        mid = (x + rng.uniform(-0.2, 0.2), 0.2, top[2] - rng.uniform(0.4, 0.8))
        end = (mid[0] + rng.uniform(-0.15, 0.15), 0.25, mid[2] - rng.uniform(0.3, 0.6))
        beam(bm, top, mid, 0.07, WOOD_DARK)
        beam(bm, mid, end, 0.05, WOOD_DARK)


def build_cave(mat):
    bm = bmesh.new()
    gallery(bm, 3.6, 3.8, 3.5)
    roots(bm, random.Random(1801), 3.6, 3.8, 5)
    return finish(bm, "CaveHole", mat, bevel=0.0)


def build_tunnel(mat):
    bm = bmesh.new()
    gallery(bm, 2.8, 3.0, 2.6)
    return finish(bm, "TunnelHole", mat, bevel=0.0)


MODELS = {"CaveHole": build_cave, "TunnelHole": build_tunnel}
