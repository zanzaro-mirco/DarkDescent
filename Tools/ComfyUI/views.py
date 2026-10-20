"""Le due foto di Blender per i modelli generati (M13, D15).

    blender --background --factory-startup --python Tools/ComfyUI/views.py -- reference Statue OUT.png
    blender --background --factory-startup --python Tools/ComfyUI/views.py -- photo Statue_Gen.glb Statue_Gen OUT.png

reference fotografa un modello di Tools/Blender di tre quarti, da usare come immagine d'ingresso di
TRELLIS.2. photo mette il GLB generato accanto al modello che dichiara COMPARE nella sua ricetta, alla
stessa altezza, con la camera del gioco.
"""
import importlib.util
import os
import pkgutil
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
BLENDER = os.path.join(HERE, "..", "Blender")
sys.path.insert(0, BLENDER)

import meshlib  # noqa: E402


def blender_builder(name):
    """La funzione che costruisce un modello di Tools/Blender, cercato per nome in tutti i moduli."""
    for info in pkgutil.iter_modules([os.path.join(BLENDER, "models")]):
        module = importlib.import_module("models." + info.name)
        if name in module.MODELS:
            return module.MODELS[name]
    sys.exit(f"[views] modello sconosciuto in Tools/Blender: {name}")


def comfy_recipe(name):
    """Il modulo di models/ che dichiara il modello generato."""
    folder = os.path.join(HERE, "models")
    for info in pkgutil.iter_modules([folder]):
        spec = importlib.util.spec_from_file_location("comfy_models_" + info.name,
                                                      os.path.join(folder, info.name + ".py"))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        if name in module.MODELS:
            return module
    sys.exit(f"[views] ricetta sconosciuta in Tools/ComfyUI/models: {name}")


def reference(name, out):
    """Di tre quarti e appena dall'alto, quadrata: TRELLIS.2 ricava meglio il volume che da davanti."""
    meshlib.reset()
    obj = blender_builder(name)(meshlib.material())
    lo, hi = meshlib.bounds([obj])
    size = max(hi - lo)
    meshlib.render(out, (lo + hi) / 2, size * 1.15, azimuth=-30.0, size=(1024, 1024))


def import_glb(path, height):
    """Il GLB alla stessa altezza del confronto. Le texture restano quelle del GLB, senza filtro netto."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    lo, hi = meshlib.bounds(meshes)
    scale = height / (hi.z - lo.z)
    for root in {o.parent if o.parent else o for o in meshes}:
        root.scale = root.scale * scale
    bpy.context.view_layer.update()
    return meshes


def photo(glb, name, out):
    module = comfy_recipe(name)
    meshlib.reset()
    original = meshlib.import_fbx(os.path.join(meshlib.ART, module.COMPARE), meshlib.material())
    olo, ohi = meshlib.bounds(original)
    generated = import_glb(glb, ohi.z - olo.z)
    glo, ghi = meshlib.bounds(generated)
    meshlib.place(original, -((ohi.x - olo.x) / 2 + 0.4))
    meshlib.place(generated, (ghi.x - glo.x) / 2 + 0.4)
    faces = sum(len(o.data.polygons) for o in generated)
    print(f"[views] {name}: {faces} facce, contro {sum(len(o.data.polygons) for o in original)}")
    meshlib.render(out, Vector(module.PHOTO["target"]), module.PHOTO["ortho"])


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) == 3 and argv[0] == "reference":
        reference(argv[1], os.path.abspath(argv[2]))
    elif len(argv) == 4 and argv[0] == "photo":
        photo(os.path.abspath(argv[1]), argv[2], os.path.abspath(argv[3]))
    else:
        sys.exit(__doc__)


main()
