"""Costruisce i modelli fatti da script (M13, D9) e li esporta in FBX per Unity.

    blender --background --factory-startup --python Tools/Blender/make.py --
            [nomi] [--out DIR] [--photo DIR]

Senza nomi li fa tutti; --out cambia la cartella degli FBX (Assets/_Project/Art/Models/DarkDescent);
--photo scrive anche, per ogni modello, una foto accanto al modello KayKit di confronto.

Ogni modulo in models/ dichiara MODELS (nome del FBX -> funzione che riceve il materiale e restituisce
l'oggetto), COMPARE (percorso sotto Art/Models/KayKit del modello di confronto e, se serve, i nomi delle
mesh da tenere) e PHOTO (centro e ampiezza dell'inquadratura); COMPARES e PHOTOS, se ci sono, li
cambiano per un modello solo.
"""
import argparse
import importlib
import os
import pkgutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from mathutils import Vector  # noqa: E402

import meshlib  # noqa: E402


def modules():
    for info in sorted(pkgutil.iter_modules([os.path.join(HERE, "models")]), key=lambda m: m.name):
        yield importlib.import_module("models." + info.name)


def photo(module, name, build, out_dir):
    meshlib.reset()
    mat = meshlib.material()
    path, keep = getattr(module, "COMPARES", {}).get(name, module.COMPARE)
    framing = getattr(module, "PHOTOS", {}).get(name, module.PHOTO)
    ours = [build(mat)]
    lo, hi = meshlib.bounds(ours)
    gap = (hi.x - lo.x) / 2 + 0.4
    original = meshlib.import_fbx(os.path.join(meshlib.KAYKIT, path), mat, keep)
    olo, ohi = meshlib.bounds(original)
    meshlib.place(original, -((ohi.x - olo.x) / 2 + 0.4))
    meshlib.place(ours, gap)
    meshlib.render(os.path.join(out_dir, name + ".png"), Vector(framing["target"]), framing["ortho"])


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(prog="make.py", description="Costruisce i modelli fatti da script.")
    parser.add_argument("names", nargs="*", help="nomi dei modelli; nessuno = tutti")
    parser.add_argument("--out", default=meshlib.OUTPUT, help="cartella degli FBX")
    parser.add_argument("--photo", help="cartella delle foto di confronto")
    args = parser.parse_args(argv)

    os.makedirs(args.out, exist_ok=True)
    known = set()
    for module in modules():
        for name, build in module.MODELS.items():
            known.add(name)
            if args.names and name not in args.names:
                continue
            meshlib.reset()
            obj = build(meshlib.material())
            lo, hi = meshlib.bounds([obj])
            meshlib.export_fbx(obj, os.path.join(args.out, name + ".fbx"))
            size = tuple(round(v, 2) for v in hi - lo)
            print(f"[blender] {name}.fbx {size[0]} x {size[1]} x {size[2]} m, {len(obj.data.polygons)} facce")
            if args.photo:
                os.makedirs(args.photo, exist_ok=True)
                photo(module, name, build, args.photo)

    unknown = set(args.names) - known
    if unknown:
        print("[blender] modelli sconosciuti: " + ", ".join(sorted(unknown)))
        sys.exit(1)


main()
