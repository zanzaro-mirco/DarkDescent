"""Gli sfondi che vengono dal gioco e non da Z-Image (M17, passo 17.7): la finitura di imagelib.py su un
render fatto dal test PhotoTour, con le ricette di RENDERS in images/backgrounds.py. Senza ComfyUI, con
il Python di ComfyUI (che ha Pillow):

    python Tools/ComfyUI/renders.py [nomi] [--out Assets/_Project/Art/Generated/Backgrounds]
"""
import argparse
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image  # noqa: E402

import imagelib  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Generated", "Backgrounds")


def renders():
    spec = importlib.util.spec_from_file_location("comfy_images_backgrounds", os.path.join(HERE, "images", "backgrounds.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.RENDERS


def main():
    recipes = renders()
    parser = argparse.ArgumentParser(prog="renders.py", description="Rifinisce gli sfondi presi dal gioco.")
    parser.add_argument("names", nargs="*", help=f"quali (di base tutti: {', '.join(recipes)})")
    parser.add_argument("--out", default=OUT, help="la cartella degli sfondi")
    args = parser.parse_args()
    unknown = set(args.names) - set(recipes)
    if unknown:
        sys.exit("nomi sconosciuti: " + ", ".join(sorted(unknown)))
    os.makedirs(args.out, exist_ok=True)
    for name in args.names or list(recipes):
        recipe = recipes[name]
        image = imagelib.finish(Image.open(os.path.join(ROOT, recipe["source"])).convert("RGB"), recipe["finish"])
        path = os.path.join(args.out, name + ".png")
        image.save(path, optimize=True)
        print(f"[renders] {path} {image.width}x{image.height}")


if __name__ == "__main__":
    main()
