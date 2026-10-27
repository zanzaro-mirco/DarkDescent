"""Le texture del terreno del borgo (M18, 18.2): prato e terra battuta, senza cuciture.

Il rumore viene da rumore bianco filtrato con la FFT, quindi è periodico per costruzione: la texture
si ripete senza bordi. Tinte piatte e macchie larghe, come i modelli KayKit, niente foto.

    python ground.py <cartella di uscita>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

SIZE = 1024


def noise(rng, scale, size=SIZE):
    """Rumore periodico tra 0 e 1, con macchie larghe circa size/scale pixel."""
    white = rng.standard_normal((size, size))
    fy = np.fft.fftfreq(size)[:, None]
    fx = np.fft.fftfreq(size)[None, :]
    radius = np.sqrt(fx * fx + fy * fy) * size
    band = np.exp(-(radius / scale) ** 2)
    field = np.real(np.fft.ifft2(np.fft.fft2(white) * band))
    field -= field.min()
    return field / field.max()


def mix(a, b, t):
    t = t[..., None]
    return a * (1 - t) + b * t


def smooth(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0, 1)
    return t * t * (3 - 2 * t)


def grass(rng):
    dark = np.array([58, 78, 40], float)
    light = np.array([104, 120, 58], float)
    dry = np.array([128, 118, 66], float)
    dirt = np.array([104, 84, 58], float)

    broad = noise(rng, 3)
    mid = noise(rng, 12)
    fine = noise(rng, 90)
    blades = noise(rng, 260)

    color = mix(dark, light, smooth(0.25, 0.75, broad * 0.6 + mid * 0.4))
    color = mix(color, dry, smooth(0.62, 0.85, noise(rng, 6)) * 0.55)
    # le zone consumate, dove la terra spunta tra l'erba
    worn = smooth(0.7, 0.82, noise(rng, 5) * 0.7 + mid * 0.3)
    color = mix(color, dirt, worn * 0.8)
    # ciuffi e fili: chiaro e scuro fini, più forti sull'erba che sulla terra
    detail = (fine - 0.5) * 34 + (blades - 0.5) * 46 * (1 - worn)
    return np.clip(color + detail[..., None], 0, 255)


def road(rng):
    base = np.array([112, 90, 64], float)
    light = np.array([142, 118, 84], float)
    dark = np.array([86, 68, 50], float)

    color = mix(base, light, smooth(0.45, 0.8, noise(rng, 6)))
    color = mix(color, dark, smooth(0.6, 0.85, noise(rng, 18)) * 0.6)
    # i solchi delle ruote e i sassolini: macchie piccole, chiare e scure
    pebbles = noise(rng, 200)
    color = mix(color, np.array([150, 144, 132], float), smooth(0.8, 0.86, pebbles))
    color = mix(color, np.array([70, 58, 46], float), smooth(0.8, 0.86, 1 - pebbles) * 0.7)
    color += ((noise(rng, 120) - 0.5) * 22)[..., None]
    return np.clip(color, 0, 255)


def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(20261027)
    Image.fromarray(grass(rng).astype(np.uint8)).save(out / 'ground_grass.png')
    Image.fromarray(road(rng).astype(np.uint8)).save(out / 'ground_road.png')
    print('ok', out)


if __name__ == '__main__':
    main()
