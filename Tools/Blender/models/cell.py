"""La cella (D9 della M13): le catene a terra con la piastra imbullonata e le manette, e la gabbia di
ferro con la porta, i cardini e il lucchetto. Il davanti è -Y."""
import math

import bmesh

from meshlib import BLACK, GOLD, STEEL, STONE_DARK, box, finish, prism, ring, torus, transform

COMPARE = ("Dungeon/keyring.fbx", None)
COMPARES = {"Cage": ("Dungeon/trunk_large_A.fbx", None)}
PHOTO = {"target": (0.0, 0.0, 0.2), "ortho": 3.2}
PHOTOS = {"Cage": {"target": (0.0, 0.0, 1.5), "ortho": 5.6}}


def chain(bm, points, size=0.09, thickness=0.03):
    """Una catena lungo una spezzata a terra: maglie alternate, una piatta e una in piedi."""
    links = []
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        length = math.hypot(x1 - x0, y1 - y0)
        n = max(1, int(length / (size * 1.6)))
        heading = math.atan2(y1 - y0, x1 - x0)
        for k in range(n):
            t = k / n
            links.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, heading))
    for k, (x, y, heading) in enumerate(links):
        upright = k % 2 == 1
        torus(bm, (x, y, size - 0.005 if upright else thickness + 0.005), size, thickness, STEEL, rot_z=heading,
              rot_x=math.pi / 2 if upright else 0.0, sides=10, segments=4)


def cuff(bm, center, rot_z):
    """Una manetta aperta, distesa a terra: una fascia di ferro e la cerniera."""
    faces = ring(bm, 12, 0.14, 0.11, 0.0, 0.07, STEEL)
    faces += box(bm, (0.14, 0, 0.04), (0.07, 0.08, 0.08), BLACK)
    transform(faces, (center[0], center[1], 0.0), rot_z=rot_z)


def build_chains(mat):
    bm = bmesh.new()
    # la piastra imbullonata con l'anello, in fondo
    box(bm, (-0.8, 0.0, 0.04), (0.36, 0.36, 0.08), STONE_DARK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            prism(bm, 6, 0.035, 0.03, 0.08, 0.11, STEEL, cx=-0.8 + sx * 0.12, cy=sy * 0.12)
    torus(bm, (-0.8, 0.0, 0.12), 0.12, 0.03, STEEL, rot_x=math.pi / 2, sides=12, segments=5)
    # due catene che si dividono, ognuna con la sua manetta
    chain(bm, [(-0.65, 0.0), (-0.2, 0.15), (0.35, 0.3), (0.75, 0.4)])
    chain(bm, [(-0.65, -0.02), (-0.15, -0.25), (0.35, -0.3)])
    cuff(bm, (0.92, 0.44), 0.3)
    cuff(bm, (0.52, -0.36), -0.6)
    return finish(bm, "Chains", mat, bevel=0.0)


def build_cage(mat):
    bm = bmesh.new()
    half = 0.8
    height = 2.2
    box(bm, (0, 0, 0.07), (2 * half + 0.16, 2 * half + 0.16, 0.14), STONE_DARK)
    box(bm, (0, 0, height - 0.06), (2 * half + 0.16, 2 * half + 0.16, 0.12), STONE_DARK)
    for z in (0.75, 1.45):   # due cerchiature a metà
        for side in (-1, 1):
            box(bm, (side * half, 0, z), (0.07, 2 * half, 0.07), STEEL)
            box(bm, (0, side * half, z), (2 * half, 0.07, 0.07), STEEL)
    bars = 5
    for i in range(bars + 1):
        t = -half + 2 * half * i / bars
        for x, y in ((t, -half), (t, half), (-half, t), (half, t)):
            box(bm, (x, y, height / 2), (0.07, 0.07, height - 0.1), STEEL)
    # la porta davanti: un telaio più spesso, due cardini e il lucchetto
    for x in (-0.33, 0.33):
        box(bm, (x, -half - 0.04, height / 2 - 0.05), (0.09, 0.06, height - 0.35), STONE_DARK)
    for z in (0.45, 1.6):
        box(bm, (-0.33, -half - 0.08, z), (0.16, 0.06, 0.12), BLACK)
    box(bm, (0.33, -half - 0.1, 1.05), (0.16, 0.08, 0.2), GOLD)
    torus(bm, (0.33, -half - 0.1, 1.2), 0.06, 0.018, GOLD, rot_x=math.pi / 2, sides=8, segments=4)
    # il tetto a punta con i pinnacoli agli angoli e l'anello per appenderla
    prism(bm, 4, half * 1.2, 0.0, height, height + 0.45, STONE_DARK, twist=math.pi / 4)
    for sx in (-1, 1):
        for sy in (-1, 1):
            prism(bm, 4, 0.06, 0.0, height, height + 0.22, STEEL, cx=sx * half, cy=sy * half, twist=math.pi / 4)
    torus(bm, (0, 0, height + 0.55), 0.12, 0.03, STEEL, rot_x=math.pi / 2, sides=12, segments=5)
    return finish(bm, "Cage", mat, bevel=0.012)


MODELS = {"Chains": build_chains, "Cage": build_cage}
