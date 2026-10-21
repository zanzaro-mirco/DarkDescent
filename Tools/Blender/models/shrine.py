"""La stele del santuario (D6): una lastra di pietra che si stringe verso l'alto, su due gradini, con una
runa d'oro incisa davanti e in cima un castone d'oro per il cristallo. Il cristallo è quello del pozzo
(Crystal), colorato in Unity dal materiale del tipo di santuario. Il davanti è -Y, come nei KayKit."""
import bmesh

from meshlib import BLACK, GOLD, STONE, STONE_DARK, box, finish, prism, transform

COMPARE = ("Dungeon/pillar.fbx", None)
PHOTO = {"target": (0.0, 0.0, 1.1), "ortho": 6.0}


def tapered_slab(bm, z0, z1, width, depth, top_scale, tile):
    """Una lastra che si stringe in alto: la scatola con i vertici di sopra avvicinati."""
    faces = box(bm, (0, 0, (z0 + z1) / 2), (width, depth, z1 - z0), tile, (0.15, 0.95))
    for v in {v for f in faces for v in f.verts}:
        if v.co.z > (z0 + z1) / 2:
            v.co.x *= top_scale
            v.co.y *= top_scale
    return faces


def build_shrine(mat):
    bm = bmesh.new()
    box(bm, (0, 0, 0.09), (1.4, 1.0, 0.18), STONE_DARK)                 # gradino di sotto
    box(bm, (0, 0, 0.25), (1.12, 0.78, 0.14), STONE)                    # gradino di sopra
    tapered_slab(bm, 0.32, 1.92, 0.82, 0.34, 0.8, STONE)                # la stele
    box(bm, (0, 0, 1.97), (0.78, 0.4, 0.1), STONE_DARK)                 # la cimasa
    # la specchiatura davanti, scura, con la runa d'oro: un rombo attraversato da un'asta
    box(bm, (0, -0.172, 1.08), (0.46, 0.02, 1.0), STONE_DARK, (0.3, 0.8))
    box(bm, (0, -0.186, 1.08), (0.05, 0.02, 0.82), GOLD)
    for sx, sz in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
        bar = box(bm, (0, 0, 0), (0.3, 0.02, 0.05), GOLD)
        transform(bar, (sx * 0.1, -0.188, 1.08 + sz * 0.1), rot_y=sx * sz * 0.785)
    box(bm, (0, -0.19, 1.08), (0.07, 0.02, 0.07), BLACK)
    # il castone: un anello d'oro con quattro griffe
    prism(bm, 6, 0.24, 0.27, 2.02, 2.1, GOLD)
    for k in range(4):
        x, y = ((0.2, 0.0), (-0.2, 0.0), (0.0, 0.2), (0.0, -0.2))[k]
        prism(bm, 4, 0.05, 0.0, 2.1, 2.32, GOLD, 0.785, x, y)
    return finish(bm, "Shrine", mat, bevel=0.018)


MODELS = {"Shrine": build_shrine}
