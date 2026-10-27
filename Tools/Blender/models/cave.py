"""La bocca della grotta del borgo e lo sbocco del cunicolo della cripta (D6 e D7 della M18), con il
davanti verso -Y. Ognuna è due modelli: l'imbocco (pali grezzi legati con la corda, la parete di massi
attorno con il masso sopra e le stalattiti, il cumulo che copre la galleria) e il buio (la galleria che
entra, grigia all'imbocco e nera in fondo). Nel gioco l'imbocco si accende sotto il cursore, il buio no.
Dalle immagini di riferimento di Mirco (27 ott 2026): sulla grotta il teschio cornuto e i paletti
appuntiti, e un braciere su treppiede (Brazier) ai lati e in fondo; il cunicolo, visto da dentro la
cripta, ha il fondo chiaro come un'uscita verso il giorno. Dalla volta della grotta pendono radici.
Il cunicolo sta al posto di un pezzo di muro della cripta (Mirco, 28 ott 2026: mai in mezzo alla
stanza): l'origine è sulla linea del muro, il fronte di massi copre i suoi 4 m e un po' dei muri
accanto fino in cima, e la galleria entra nella roccia dietro."""
import math
import random

import bmesh

from meshlib import (BLACK, BURLAP, FLAME, GOLD, PAPER, STEEL, STONE, STONE_DARK, WHITE, WOOD, WOOD_DARK, beam,
                     box, finish, paint, prism, transform)

COMPARE = ("Dungeon/wall.fbx", None)
PHOTO = {"target": (2.6, 0.0, 1.8), "ortho": 11.0}
PHOTOS = {"TunnelEntrance": {"target": (2.6, 0.0, 2.0), "ortho": 10.0},
          "TunnelHole": {"target": (2.2, 0.0, 1.4), "ortho": 9.0},
          "Brazier": {"target": (0.6, 0.0, 0.6), "ortho": 3.0}}

# larghezza, altezza e profondità del buio; l'imbocco ci sta attorno
CAVE = (3.0, 3.2, 4.0)
TUNNEL = (2.3, 2.6, 2.4)

# il cunicolo nel muro: il muro della cripta è largo 4, spesso 1 e alto 4, con la faccia verso la
# stanza a -0,5; l'imbocco sta appena davanti, il fronte di massi è largo SPAN
TUNNEL_FRONT = -0.6
SPAN = 5.6

# quanto della galleria, dall'imbocco, resta grigio scuro prima del nero
LIT = 0.8


def profile(width, height, sides=7):
    """Pareti dritte fino a tre quarti, poi una volta bassa: un passaggio scavato, non una nicchia."""
    wall = height * 0.75
    points = [(-width / 2, 0.0), (-width / 2, wall)]
    for k in range(1, sides):
        t = math.pi - math.pi * k / sides
        points.append((math.cos(t) * width / 2, wall + math.sin(t) * (height - wall)))
    return points + [(width / 2, wall), (width / 2, 0.0)]


def gallery(bm, width, height, depth, daylight=False, start=0.0):
    """Anelli lungo la profondità, sempre più stretti e bassi: l'imbocco grigio scuro, poi il nero, e il
    fondo chiuso. Le facce guardano dentro: da fuori si vede solo attraverso l'imbocco. Con daylight il
    fondo è chiaro e l'ultimo tratto schiarisce: di là c'è il giorno."""
    shape = profile(width, height)
    stops = [0.0, LIT, LIT + (depth - LIT) * 0.4, depth]
    rings = []
    for y in stops:
        s = 1.0 - 0.3 * y / depth
        rings.append([bm.verts.new((x * s, start + y, z * (1.0 - 0.25 * y / depth))) for x, z in shape])
    for j in range(len(rings) - 1):
        a, b = rings[j], rings[j + 1]
        faces = [bm.faces.new((a[i], b[i], b[i + 1], a[i + 1])) for i in range(len(shape) - 1)]
        faces.append(bm.faces.new((a[0], a[-1], b[-1], b[0])))
        if j == 0:
            paint(bm, faces, STONE_DARK, -height * 0.6, height)
        elif daylight and j == len(rings) - 2:
            paint(bm, faces, STONE_DARK, -height, height * 0.5)
        else:
            paint(bm, faces, BLACK, 0.0, height)
    paint(bm, [bm.faces.new(rings[-1])], WHITE if daylight else BLACK, -height * 0.6, height)


def roots(bm, rng, width, height, count):
    """Radici che pendono dalla volta, appena dentro."""
    for _ in range(count):
        x = rng.uniform(-width * 0.35, width * 0.35)
        top = (x, 0.35, height * 0.97)
        mid = (x + rng.uniform(-0.2, 0.2), 0.3, top[2] - rng.uniform(0.4, 0.8))
        end = (mid[0] + rng.uniform(-0.15, 0.15), 0.35, mid[2] - rng.uniform(0.3, 0.5))
        beam(bm, top, mid, 0.07, WOOD_DARK)
        beam(bm, mid, end, 0.05, WOOD_DARK)


def rock(bm, rng, center, size, tile, jitter=0.2):
    """Un masso a facce, come i sassi KayKit: tre anelli di sei o sette lati, girati e spostati a caso,
    più stretto in cima, con il tetto piatto."""
    sides = rng.choice((6, 7))
    twist = rng.uniform(0, 2 * math.pi)
    sx, sy, sz = (s / 2 for s in size)
    cx, cy, cz = center
    rings = []
    for k, (r, z) in enumerate(((0.85, -1.0), (1.0, -0.15), (0.62, 1.0))):
        ring = []
        for i in range(sides):
            a = twist + 2 * math.pi * (i + 0.5 * k) / sides
            rr = r * (1 + rng.uniform(-jitter, jitter))
            ring.append(bm.verts.new((cx + math.cos(a) * rr * sx, cy + math.sin(a) * rr * sy,
                                      cz + (z + rng.uniform(-jitter, jitter) * 0.4) * sz)))
        rings.append(ring)
    faces = [bm.faces.new(list(reversed(rings[0]))), bm.faces.new(rings[-1])]
    for j in range(2):
        a, b = rings[j], rings[j + 1]
        for i in range(sides):
            n = (i + 1) % sides
            faces.append(bm.faces.new((a[i], a[n], b[n])))
            faces.append(bm.faces.new((a[i], b[n], b[i])) if j == 0 else bm.faces.new((a[i], b[n], b[i])))
    paint(bm, faces, tile, cz - sz, cz + sz)
    return faces


def pole(bm, rng, p0, p1, thickness):
    """Un palo grezzo: due tronchi un po' storti, legati dove si toccano."""
    mid = [(a + b) / 2 + rng.uniform(-0.05, 0.05) for a, b in zip(p0, p1)]
    beam(bm, p0, mid, thickness, WOOD)
    beam(bm, mid, p1, thickness * 0.92, WOOD)


def rope(bm, center, size):
    box(bm, center, size, BURLAP)


def timber(bm, rng, width, height, front=0.0, overhang=0.55):
    """Due pali grezzi un po' inclinati e la traversa che sporge, legati con la corda: il legno è la
    prima cosa che dice "qui si entra"."""
    top = height * 0.82
    for side in (-1, 1):
        x = side * (width / 2 - 0.1)
        pole(bm, rng, (x + side * 0.12, front - 0.15, 0.0), (x, front - 0.12, top + 0.25), 0.34)
        rope(bm, (x, front - 0.13, top), (0.42, 0.42, 0.14))
    pole(bm, rng, (-width / 2 - overhang, front - 0.2, top + 0.12), (width / 2 + overhang, front - 0.2, top + 0.2), 0.36)
    return top + 0.16


def beast_skull(bm, center, s=1.4):
    """Il teschio cornuto sopra la traversa (prima immagine di Mirco): cranio, muso, orbite nere e due
    corna che salgono di lato."""
    cx, cy, cz = center
    box(bm, (cx, cy, cz + 0.32 * s), (0.62 * s, 0.5 * s, 0.5 * s), WHITE)
    box(bm, (cx, cy - 0.28 * s, cz + 0.12 * s), (0.36 * s, 0.42 * s, 0.32 * s), PAPER)
    for side in (-1, 1):
        box(bm, (cx + side * 0.15 * s, cy - 0.26 * s, cz + 0.36 * s), (0.13 * s, 0.04 * s, 0.13 * s), BLACK)
        a = (cx + side * 0.28 * s, cy, cz + 0.45 * s)
        b = (cx + side * 0.62 * s, cy + 0.05 * s, cz + 0.6 * s)
        c = (cx + side * 0.82 * s, cy - 0.05 * s, cz + 0.95 * s)
        d = (cx + side * 0.78 * s, cy - 0.15 * s, cz + 1.25 * s)
        beam(bm, a, b, 0.16 * s, WHITE)
        beam(bm, b, c, 0.12 * s, WHITE)
        beam(bm, c, d, 0.08 * s, PAPER)


def stalactites(bm, rng, width, z, count, y=0.0):
    """Punte di roccia che pendono dal masso sopra l'imbocco."""
    for k in range(count):
        x = -width / 2 + width * (k + 0.5) / count + rng.uniform(-0.12, 0.12)
        length = rng.uniform(0.3, 0.75)
        faces = prism(bm, 5, rng.uniform(0.1, 0.17), 0.0, 0.0, length, STONE, twist=rng.uniform(0, 1))
        transform(faces, (x, y + rng.uniform(0.0, 0.25), z), rot_x=math.pi)


def wall_face(bm, rng, width, height):
    """Il fronte di massi al posto del pezzo di muro: gli stipiti attorno all'imbocco, una colonna
    più in là sopra la giunta con il muro accanto, il masso sopra con le stalattiti e uno in cima,
    alti quanto il muro (4 m)."""
    w = width / 2
    for side in (-1, 1):
        z = 0.0
        for size in (1.35, 1.25, 1.15, 1.05):
            rock(bm, rng, (side * (w + size * 0.5), -0.35 + rng.uniform(-0.08, 0.08), z + size * 0.45),
                 (size, size * 0.9, size * 0.95), STONE, jitter=0.15)
            z += size * 0.8
        z = 0.0
        for k, size in enumerate((1.0, 0.95, 0.9, 0.9, 0.8)):
            rock(bm, rng, (side * (SPAN / 2 - 0.45), -0.1 + rng.uniform(-0.08, 0.08), z + size * 0.45),
                 (size * 0.9, size * 0.8, size), STONE_DARK if k % 2 else STONE, jitter=0.15)
            z += size * 0.85
    rock(bm, rng, (0.0, -0.45, height + 0.45), (width + 1.4, 1.2, 1.0), STONE, jitter=0.12)
    rock(bm, rng, (rng.uniform(-0.3, 0.3), -0.2, height + 1.3), (width + 0.8, 1.1, 0.9), STONE_DARK, jitter=0.15)
    stalactites(bm, rng, width, height + 0.02, 6, y=-0.75)


def hood(bm, width, height, y0, y1):
    """Un cappuccio nero sopra e ai lati della galleria, dentro la roccia: le facce della galleria
    guardano dentro, e senza la camera dall'alto ci vedrebbe attraverso. Nero come il vuoto oltre i
    muri. Davanti è aperto: dall'imbocco si vede la galleria."""
    w = width / 2
    a, b = bm.verts.new((-w, y0, 0.0)), bm.verts.new((-w, y1, 0.0))
    c, d = bm.verts.new((-w, y1, height)), bm.verts.new((-w, y0, height))
    e, f = bm.verts.new((w, y0, height)), bm.verts.new((w, y1, height))
    g, h = bm.verts.new((w, y1, 0.0)), bm.verts.new((w, y0, 0.0))
    faces = [bm.faces.new((a, d, c, b)), bm.faces.new((d, e, f, c)), bm.faces.new((e, h, g, f)), bm.faces.new((b, c, f, g))]
    paint(bm, faces, BLACK, 0.0, height)


def stakes(bm, rng, x, y, count):
    """Un mucchio di paletti appuntiti piantati per terra, un po' storti, verso l'esterno."""
    for k in range(count):
        px, py = x + rng.uniform(-0.45, 0.45), y + rng.uniform(-0.3, 0.3)
        h = rng.uniform(1.0, 1.7)
        lean = rng.uniform(-0.25, 0.25)
        beam(bm, (px, py, 0.0), (px + lean, py - 0.15, h), 0.13, WOOD_DARK if k % 2 else WOOD)
        faces = prism(bm, 4, 0.08, 0.0, 0.0, 0.25, WOOD, twist=0.78)
        transform(faces, (px + lean * 1.02, py - 0.15, h))


def frame(bm, rng, width, height, depth, debris=True):
    """La parete di massi: stipiti sui lati, il masso sopra che sporge con le stalattiti, il cumulo
    sulla galleria che scende verso il fondo, e i detriti davanti, fuori dal passaggio (non nel
    cunicolo, dove ritaglierebbero il NavMesh attorno al punto d'arrivo)."""
    w, h = width / 2, height
    # gli stipiti: due o tre massi l'uno sull'altro per lato, dietro i puntelli
    for side in (-1, 1):
        z = 0.0
        for k, size in enumerate((1.6, 1.35, 1.1)):
            if z > h:
                break
            rock(bm, rng, (side * (w + size * 0.38), 0.35 + rng.uniform(0.0, 0.2), z + size * 0.45),
                 (size * 1.1, size, size * 0.95), STONE, jitter=0.18)
            z += size * 0.78
    # il masso sopra l'imbocco, più largo, e uno più in alto
    rock(bm, rng, (0.0, 0.35, h + 0.5), (width + 1.6, 1.4, 1.1), STONE, jitter=0.12)
    rock(bm, rng, (rng.uniform(-0.4, 0.4), 0.9, h + 1.25), (width * 0.8, 1.2, 0.9), STONE_DARK, jitter=0.2)
    stalactites(bm, rng, width, h + 0.05, 6)
    # il cumulo sulla galleria e sui suoi fianchi: più basso verso il fondo
    rows = max(3, int(depth / 0.9) + 1)
    for r in range(rows):
        y = 0.9 + (depth - 0.4) * r / (rows - 1)
        top = h + 0.7 - 1.1 * r / (rows - 1)
        for side in (-1, 0, 1):
            size = rng.uniform(1.3, 1.7)
            x = side * (w + 0.35)
            z = top - size * 0.4 if side == 0 else size * 0.45
            rock(bm, rng, (x + rng.uniform(-0.2, 0.2), y + rng.uniform(-0.2, 0.2), max(z, size * 0.4)),
                 (size * 1.2, size * 1.2, size * (1.0 if side == 0 else 1.6)), STONE if (r + side) % 3 else STONE_DARK, jitter=0.2)
    # i detriti davanti, ai lati del passaggio
    for k in range(9 if debris else 0):
        side = -1 if k % 2 else 1
        x = side * rng.uniform(w * 0.7, w + 1.1)
        s = rng.uniform(0.2, 0.45)
        rock(bm, rng, (x, rng.uniform(-1.3, -0.3), s * 0.3), (s, s * 0.9, s * 0.6), STONE)


def build_cave_entrance(mat):
    bm = bmesh.new()
    rng = random.Random(1804)
    width, height, depth = CAVE
    top = timber(bm, rng, width, height)
    beast_skull(bm, (0.0, -0.3, top))
    frame(bm, rng, width, height, depth)
    for side in (-1, 1):
        stakes(bm, rng, side * (width / 2 + 1.9), -0.4, 4)
    return finish(bm, "CaveEntrance", mat)


def build_cave(mat):
    bm = bmesh.new()
    width, height, depth = CAVE
    gallery(bm, width, height, depth)
    roots(bm, random.Random(1801), width, height, 5)
    return finish(bm, "CaveHole", mat, bevel=0.0)


def build_tunnel_entrance(mat):
    """Il fronte nel muro con i pali e il teschio cornuto davanti, come la grotta del borgo."""
    bm = bmesh.new()
    rng = random.Random(1807)
    width, height, depth = TUNNEL
    top = timber(bm, rng, width, height, front=-0.95, overhang=0.85)
    beast_skull(bm, (0.0, -1.2, top), s=1.2)
    wall_face(bm, rng, width, height)
    hood(bm, width + 0.6, height + 0.4, 0.4, TUNNEL_FRONT + depth + 0.1)
    return finish(bm, "TunnelEntrance", mat)


def build_tunnel(mat):
    bm = bmesh.new()
    gallery(bm, *TUNNEL, daylight=True, start=TUNNEL_FRONT)
    return finish(bm, "TunnelHole", mat, bevel=0.0)


def build_brazier(mat):
    """Il braciere su treppiede (prima immagine di Mirco): tre gambe legate, un cesto di ferro e le
    braci con le fiamme. La luce e il tremolio li mette il prefab, sopra le fiamme."""
    bm = bmesh.new()
    rng = random.Random(1809)
    for k in range(3):
        a = 2 * math.pi * k / 3
        beam(bm, (math.cos(a) * 0.42, math.sin(a) * 0.42, 0.0), (math.cos(a) * 0.12, math.sin(a) * 0.12, 0.95), 0.08, WOOD)
    rope(bm, (0.0, 0.0, 0.72), (0.26, 0.26, 0.08))
    prism(bm, 8, 0.2, 0.34, 0.9, 1.15, STEEL)
    prism(bm, 8, 0.3, 0.3, 1.13, 1.18, BLACK)
    for k in range(5):
        a = 2 * math.pi * k / 5 + rng.uniform(-0.3, 0.3)
        r = 0.12 if k else 0.0
        prism(bm, 5, rng.uniform(0.09, 0.13), 0.0, 1.15, 1.15 + rng.uniform(0.3, 0.55), FLAME,
              twist=rng.uniform(0, 1), cx=math.cos(a) * r, cy=math.sin(a) * r)
    prism(bm, 5, 0.06, 0.0, 1.15, 1.32, GOLD)
    return finish(bm, "Brazier", mat, bevel=0.01)


MODELS = {"CaveEntrance": build_cave_entrance, "CaveHole": build_cave,
          "TunnelEntrance": build_tunnel_entrance, "TunnelHole": build_tunnel, "Brazier": build_brazier}
