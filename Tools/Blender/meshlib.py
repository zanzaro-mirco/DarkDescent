"""Libreria comune dei modelli fatti in Blender da script (M13, D9). Gira dentro Blender 5.2 LTS.

I colori vengono dall'atlante KayKit `dungeon_texture.png`: 8 x 4 riquadri a gradiente, chiaro in
alto e scuro in basso. Ogni faccia prende un riquadro, e la v segue l'altezza, come nei modelli KayKit:
così un modello nostro sta accanto a un KayKit con lo stesso materiale (`M_Dungeon`).
"""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
ART = os.path.join(ROOT, "Assets", "_Project", "Art", "Models")
KAYKIT = os.path.join(ART, "KayKit")
OUTPUT = os.path.join(ART, "DarkDescent")
ATLAS = os.path.join(KAYKIT, "Dungeon", "dungeon_texture.png")

# Riquadri dell'atlante: (colonna, riga dall'alto)
STONE_DARK = (0, 0)
STEEL = (1, 0)
WOOD_LIGHT = (2, 0)
BLACK = (3, 0)
WOOD = (4, 0)
STONE = (5, 0)
WOOD_DARK = (7, 0)
GREEN = (2, 2)
RED = (3, 2)
GOLD = (5, 2)
PURPLE = (6, 2)
BLUE = (2, 3)


# ---------------------------------------------------------------- scena e materiale

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name="Atlas", image=ATLAS):
    """Il materiale delle foto: la texture con i pixel netti. In Unity il modello prende M_Dungeon."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(image, check_existing=True)
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.9
    return mat


# ---------------------------------------------------------------- colori

def paint(bm, faces, tile, z0, z1, u_shift=0.4):
    """UV delle facce sul riquadro: u fisso, v che segue l'altezza tra z0 e z1 (chiaro in alto)."""
    uv = bm.loops.layers.uv.verify()
    col, row = tile
    u = (col + u_shift) / 8.0
    vmin = 1.0 - (row + 1) / 4.0
    for face in faces:
        for loop in face.loops:
            t = 0.5 if z1 <= z0 else min(1.0, max(0.0, (loop.vert.co.z - z0) / (z1 - z0)))
            loop[uv].uv = (u, vmin + (0.06 + 0.88 * t) / 4.0)


# ---------------------------------------------------------------- forme

def _circle(bm, sides, r, z, twist=0.0, cx=0.0, cy=0.0):
    return [bm.verts.new((cx + r * math.cos(twist + 2 * math.pi * i / sides),
                          cy + r * math.sin(twist + 2 * math.pi * i / sides), z)) for i in range(sides)]


def box(bm, center, size, tile, shade=(0.0, 1.0)):
    """Una scatola; shade dice quale parte del gradiente usa (0 scuro, 1 chiaro)."""
    cx, cy, cz = center
    sx, sy, sz = (s / 2 for s in size)
    v = [bm.verts.new((cx + x * sx, cy + y * sy, cz + z * sz)) for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)]
    idx = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    faces = [bm.faces.new([v[i] for i in f]) for f in idx]
    span = max(1e-6, shade[1] - shade[0])
    z0 = cz - sz - (sz * 2) * shade[0] / span
    paint(bm, faces, tile, z0, z0 + (sz * 2) / span)
    return faces


def rbox(bm, center, size, tile, rot_z=0.0, rot_x=0.0, shade=(0.0, 1.0)):
    """Una scatola girata attorno a Z e poi a X, centrata in center."""
    faces = box(bm, (0, 0, 0), size, tile, shade)
    m = Matrix.Translation(Vector(center)) @ Matrix.Rotation(rot_z, 4, "Z") @ Matrix.Rotation(rot_x, 4, "X")
    for vert in {v for f in faces for v in f.verts}:
        vert.co = m @ vert.co
    return faces


def lathe(bm, profile, sides, tile_of, cap_top=None, cap_bottom=None, twist=0.0):
    """Un solido di rivoluzione dal profilo [(r, z)]; tile_of(lato, anello) dà il riquadro di ogni faccia."""
    rings = [_circle(bm, sides, r, z, twist) for r, z in profile]
    z0, z1 = profile[0][1], profile[-1][1]
    for j in range(len(rings) - 1):
        for i in range(sides):
            k = (i + 1) % sides
            f = bm.faces.new((rings[j][i], rings[j][k], rings[j + 1][k], rings[j + 1][i]))
            paint(bm, [f], tile_of(i, j), z0, z1)
    if cap_top is not None:
        paint(bm, [bm.faces.new(rings[-1])], cap_top, z0, z1)
    if cap_bottom is not None:
        paint(bm, [bm.faces.new(list(reversed(rings[0])))], cap_bottom, z0, z1)
    return rings


def ring(bm, sides, r_out, r_in, z0, z1, tile, twist=0.0):
    """Un anello prismatico: pareti esterne e interne, sopra e sotto."""
    ob, ot = _circle(bm, sides, r_out, z0, twist), _circle(bm, sides, r_out, z1, twist)
    ib, it = _circle(bm, sides, r_in, z0, twist), _circle(bm, sides, r_in, z1, twist)
    faces = []
    for i in range(sides):
        j = (i + 1) % sides
        faces.append(bm.faces.new((ob[i], ob[j], ot[j], ot[i])))
        faces.append(bm.faces.new((ib[j], ib[i], it[i], it[j])))
        faces.append(bm.faces.new((ot[i], ot[j], it[j], it[i])))
        faces.append(bm.faces.new((ob[j], ob[i], ib[i], ib[j])))
    paint(bm, faces, tile, z0, z1)
    return faces


def prism(bm, sides, r_bottom, r_top, z0, z1, tile, twist=0.0, cx=0.0, cy=0.0):
    """Un prisma o un tronco pieno, chiuso sopra e sotto; r_top 0 fa una punta."""
    b = _circle(bm, sides, r_bottom, z0, twist, cx, cy)
    if r_top <= 0:
        tip = bm.verts.new((cx, cy, z1))
        faces = [bm.faces.new((b[i], b[(i + 1) % sides], tip)) for i in range(sides)]
    else:
        t = _circle(bm, sides, r_top, z1, twist, cx, cy)
        faces = [bm.faces.new(t)]
        faces += [bm.faces.new((b[i], b[(i + 1) % sides], t[(i + 1) % sides], t[i])) for i in range(sides)]
    faces.append(bm.faces.new(list(reversed(b))))
    paint(bm, faces, tile, z0, z1)
    return faces


def crystal(bm, r, z0, zmid, z1, tile, sides=6, cx=0.0, cy=0.0):
    """Un cristallo allungato: due punte unite a metà."""
    mid = _circle(bm, sides, r, zmid, 0.0, cx, cy)
    bottom, top = bm.verts.new((cx, cy, z0)), bm.verts.new((cx, cy, z1))
    faces = []
    for i in range(sides):
        j = (i + 1) % sides
        faces.append(bm.faces.new((mid[i], mid[j], top)))
        faces.append(bm.faces.new((mid[j], mid[i], bottom)))
    paint(bm, faces, tile, z0, z1)
    return faces


def finish(bm, name, mat, bevel=0.022):
    """Spigoli smussati come nei KayKit, facce piatte, un oggetto nella scena."""
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=1, affect="EDGES", clamp_overlap=True)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = False
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ---------------------------------------------------------------- confronto e foto

def import_fbx(path, mat, keep=None):
    """Un FBX (di solito KayKit) con il materiale dato; keep tiene solo le mesh con quei nomi."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = []
    for o in [o for o in bpy.data.objects if o not in before]:
        if o.type == "MESH" and (keep is None or o.name in keep):
            o.data.materials.clear()
            o.data.materials.append(mat)
            meshes.append(o)
        elif o.type == "MESH":
            bpy.data.objects.remove(o)
    return meshes


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    return (Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
            Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))))


def place(objs, x):
    """Sposta un gruppo: centro in x, centro in y, base a z 0."""
    lo, hi = bounds(objs)
    for r in {o.parent if o.parent else o for o in objs}:
        r.location.x += x - (lo.x + hi.x) / 2
        r.location.y -= (lo.y + hi.y) / 2
        r.location.z -= lo.z
    bpy.context.view_layer.update()


def render(path, target, ortho, azimuth=-45.0, size=(1100, 650)):
    """Foto con la camera del gioco: ortografica, 30 gradi sopra, 45 di lato."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = size
    world = bpy.data.worlds.new("W")
    world.color = (0.08, 0.07, 0.07)
    scene.world = world
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    d, el, az = 30.0, math.radians(30), math.radians(azimuth)
    cam.location = target + Vector((d * math.cos(el) * math.sin(az), -d * math.cos(el) * math.cos(az), d * math.sin(el)))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------- export

def export_fbx(obj, path):
    """Solo l'oggetto, con gli assi di Unity e la trasformazione applicata: in Unity arriva con
    rotazione zero e scala 1 (la trappola del chest_gold della M12)."""
    for o in bpy.data.objects:
        o.select_set(o == obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        path_mode="STRIP",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
