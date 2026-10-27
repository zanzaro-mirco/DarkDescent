"""L'atlante delle macchie a terra (M17, passo 17.5), senza ComfyUI: le otto immagini di images/decals.py,
nell'ordine della ricetta, in una griglia da 4 colonne e caselle da 256. Si lancia dopo make.py, con il
Python di ComfyUI:

    python Tools/ComfyUI/decalatlas.py [--folder Tools/ComfyUI/out/images/decals] [--out ...]
"""
import argparse
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image  # noqa: E402

import imagelib  # noqa: E402

FOLDER = os.path.join(HERE, "out", "images", "decals")
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "Generated", "Decals",
                                    "decal_atlas.png"))

# 4 colonne da 256: un atlante da 1024x512 (D12)
COLUMNS = 4
CELL = 256


def names():
    spec = importlib.util.spec_from_file_location("comfy_images_decals", os.path.join(HERE, "images", "decals.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return list(module.IMAGES)


def main():
    parser = argparse.ArgumentParser(prog="decalatlas.py", description="Compone l'atlante delle macchie.")
    parser.add_argument("--folder", default=FOLDER, help="la cartella con le macchie generate")
    parser.add_argument("--out", default=OUT, help="il PNG dell'atlante")
    args = parser.parse_args()
    tiles = [Image.open(os.path.join(args.folder, name + ".png")) for name in names()]
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    imagelib.atlas(tiles, COLUMNS, CELL).save(args.out, optimize=True)
    print(f"[decals] {args.out}")


if __name__ == "__main__":
    main()
