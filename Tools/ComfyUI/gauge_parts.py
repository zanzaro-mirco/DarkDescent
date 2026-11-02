"""Gli indicatori di vita e mana (M20, D18): dalla cornice di Z-Image (ricetta gauge_skull di images/hud.py)
fa i quattro pezzi del gioco: la cornice col vetro svuotato, il vetro scuro e i due liquidi.

    python Tools/ComfyUI/make.py gauge_skull --out DIR
    python Tools/ComfyUI/gauge_parts.py DIR/gauge_skull.png Assets/_Project/Art/Generated/UI

La finestra del vetro è a mano, in pixel dell'immagine (145 x 480): se la cornice cambia vanno rimisurate
e riportate in Assets/_Project/Editor/HudTools.cs. Si lancia con il Python di ComfyUI, che ha Pillow.
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw

WINDOW = (36, 88, 99, 413)
RADIUS = 10
OVERLAP = 2  # il liquido e il vetro entrano di 2 pixel sotto la cornice, senza buchi
RED = ((205, 34, 30), (96, 6, 8))
BLUE = ((54, 96, 226), (14, 24, 112))
GLASS = ((36, 30, 28), (12, 10, 10))


def gradient(w, h, top, bottom):
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(1, h - 1)
        d.line((0, y, w, y), fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return img


def rounded(w, h, radius):
    mask = Image.new("L", (w * 4, h * 4), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w * 4 - 1, h * 4 - 1), radius=radius * 4, fill=255)
    return mask.resize((w, h), Image.LANCZOS)


def part(colors, w, h, shine):
    img = gradient(w, h, *colors)
    if shine:
        # il liquido ha un po' di luce a sinistra e le ombre ai bordi
        glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        gd = ImageDraw.Draw(glow)
        gd.rounded_rectangle((6, 6, 11, h - 14), radius=3, fill=(255, 255, 255, 46))
        gd.rectangle((w - 9, 0, w, h), fill=(0, 0, 0, 50))
        img.alpha_composite(glow)
    img.putalpha(rounded(w, h, RADIUS + OVERLAP))
    return img


def main(source, out):
    frame = Image.open(source).convert("RGBA")
    x0, y0, x1, y1 = WINDOW
    hole = Image.new("L", frame.size, 0)
    ImageDraw.Draw(hole).rounded_rectangle((x0, y0, x1, y1), radius=RADIUS, fill=255)
    frame.putalpha(ImageChops.subtract(frame.getchannel("A"), hole))
    # il riflesso del vetro, sopra tutto
    shine = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    ImageDraw.Draw(shine).rounded_rectangle((x0 + 5, y0 + 10, x0 + 11, y0 + (y1 - y0) * 0.45), radius=3, fill=(255, 255, 255, 58))
    frame.alpha_composite(shine)
    frame.save(os.path.join(out, "gauge_frame.png"))

    w, h = x1 - x0 + 2 * OVERLAP, y1 - y0 + 2 * OVERLAP
    part(GLASS, w, h, False).save(os.path.join(out, "gauge_glass.png"))
    part(RED, w, h, True).save(os.path.join(out, "gauge_life.png"))
    part(BLUE, w, h, True).save(os.path.join(out, "gauge_mana.png"))
    print(f"gauge_frame {frame.size}, parti {w}x{h}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
