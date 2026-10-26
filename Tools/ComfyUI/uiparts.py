"""Le parti dell'interfaccia ricavate dalle immagini generate (M17, passo 17.3), senza ComfyUI: la
placca dei pulsanti dallo sfondo dei pannelli. Si lancia dopo make.py, con il Python di ComfyUI:

    python Tools/ComfyUI/uiparts.py [--folder Assets/_Project/Art/Generated/UI]
"""
import argparse
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from PIL import Image  # noqa: E402

import imagelib  # noqa: E402

FOLDER = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "Generated", "UI"))

# la placca: 48 pixel, bordo di 6 (il bordo delle nove parti nell'import di Unity)
PLATE_SIZE = 48
PLATE_BORDER = 6


def main():
    parser = argparse.ArgumentParser(prog="uiparts.py", description="Ricava le parti dell'interfaccia.")
    parser.add_argument("--folder", default=FOLDER, help="la cartella con panel_back.png")
    args = parser.parse_args()
    back = Image.open(os.path.join(args.folder, "panel_back.png"))
    path = os.path.join(args.folder, "button_plate.png")
    imagelib.button_plate(back, PLATE_SIZE, PLATE_BORDER).save(path, optimize=True)
    print(f"[ui] {path}")


if __name__ == "__main__":
    main()
