"""La finitura delle immagini generate (M17, D8 e D9): cuciture, trasparenza, ritaglio, dimensione.

Solo Pillow. Le funzioni non sanno niente di ComfyUI: make.py genera l'immagine, le chiede di misurare
le cuciture, ridisegna la croce e poi applica i passi di finitura della ricetta.
"""
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageStat

# oltre questo rapporto le cuciture si vedono: un'immagine che si ripete sta tra 1 e 1,3, una con le
# cuciture visibili oltre 2,5 (prove del passo 17.2)
SEAM_LIMIT = 1.8


def _gray(image):
    return image.convert("L")


def _column_difference(gray, a, b):
    """La differenza media tra due colonne di pixel."""
    h = gray.height
    left = gray.crop((a, 0, a + 1, h))
    right = gray.crop((b, 0, b + 1, h))
    return ImageStat.Stat(ImageChops.difference(left, right)).mean[0]


def seam_score(image):
    """Quanto si vedono le cuciture ripetendo l'immagine: la differenza tra i bordi opposti, e quella
    sulla linea di mezzo (dove un'immagine spostata di mezzo lato nasconde la sua cucitura), contro
    quella media tra due colonne (o righe) vicine. Circa 1 se si ripete senza cuciture, molto di più
    se no. Il valore è il peggiore dei due assi."""
    gray = _gray(image)
    scores = []
    for g in (gray, gray.transpose(Image.Transpose.ROTATE_90)):
        w = g.width
        edge = _column_difference(g, w - 1, 0)
        middle = _column_difference(g, w // 2 - 1, w // 2)
        inside = [_column_difference(g, x, x + 1) for x in range(w // 8, w - w // 8, max(1, w // 32))]
        typical = max(sum(inside) / len(inside), 0.5)
        scores.append(max(edge, middle) / typical)
    return max(scores)


def offset_half(image):
    """L'immagine spostata di mezzo lato: le cuciture finiscono in una croce al centro."""
    return ImageChops.offset(image, image.width // 2, image.height // 2)


def cross_mask(size, band, feather):
    """La maschera della croce al centro, larga band pixel, sfumata di feather: bianco si ridisegna."""
    w, h = size
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rectangle((w // 2 - band // 2, 0, w // 2 + band // 2, h), fill=255)
    draw.rectangle((0, h // 2 - band // 2, w, h // 2 + band // 2), fill=255)
    return mask.filter(ImageFilter.GaussianBlur(feather)) if feather > 0 else mask


def border_color(image):
    """Il colore del fondo letto dai bordi: la mediana dei pixel lungo il contorno. Il modello non dà
    mai il colore chiesto preciso (un magenta esce bordeaux o viola)."""
    rgb = image.convert("RGB")
    w, h = rgb.size
    px = rgb.load()
    step = max(1, min(w, h) // 64)
    ring = [px[x, y] for x in range(0, w, step) for y in (0, h - 1)]
    ring += [px[x, y] for y in range(0, h, step) for x in (0, w - 1)]
    return tuple(sorted(c[i] for c in ring)[len(ring) // 2] for i in range(3))


def blend_seams(image, band, feather):
    """Ripetibile senza il modello, per i materiali uniformi: l'immagine spostata di mezzo lato ha i
    bordi buoni e la cucitura in una croce al centro, l'originale il contrario. Si prende l'originale
    lungo la croce e la spostata altrove, sfumando in mezzo."""
    rgb = image.convert("RGB")
    shifted = offset_half(rgb)
    mask = cross_mask(rgb.size, band, feather)
    return Image.composite(rgb, shifted, mask)


def key_out(image, color="auto", low=0.15, high=0.55):
    """La trasparenza da un fondo di un colore acceso (magenta, verde): conta la tinta, non la luce.
    Ogni pixel si misura lungo la tinta del fondo, da 0 (un grigio, anche scurissimo, resta pieno) a 1
    (il fondo): sotto low è pieno, sopra high trasparente, in mezzo sfumato. Dai pixel che restano si
    toglie la parte della tinta del fondo, così i bordi non hanno l'alone. Il colore "auto" si legge
    dai bordi dell'immagine."""
    if color == "auto":
        bg = border_color(image)
    elif isinstance(color, str):
        bg = tuple(int(color[i:i + 2], 16) for i in (1, 3, 5))
    else:
        bg = tuple(color)
    mean = sum(bg) / 3
    key = [c - mean for c in bg]
    norm = sum(k * k for k in key)
    if norm < 400:
        raise ValueError(f"il fondo {bg} non ha una tinta: serve un colore acceso, non un grigio")
    rgb = image.convert("RGB")
    out = Image.new("RGBA", rgb.size)
    src = rgb.load()
    dst = out.load()
    for y in range(rgb.height):
        for x in range(rgb.width):
            pixel = src[x, y]
            m = sum(pixel) / 3
            chroma = [c - m for c in pixel]
            s = sum(c * k for c, k in zip(chroma, key)) / norm
            a = 1.0 if s <= low else 0.0 if s >= high else 1.0 - (s - low) / (high - low)
            if a <= 0.0:
                dst[x, y] = (0, 0, 0, 0)
                continue
            # despill: via la parte della tinta del fondo, il resto del colore non cambia
            spill = max(0.0, s)
            clean = tuple(max(0, min(255, round(c - spill * k))) for c, k in zip(pixel, key))
            dst[x, y] = clean + (round(a * 255),)
    return out


def flatten(image, fraction=0.125):
    """Spiana la luce: divide l'immagine per la sua versione molto sfocata e rimette la luminosità
    media. Toglie la luce più chiara al centro che il modello aggiunge, che ripetuta fa una scacchiera."""
    rgb = image.convert("RGB")
    radius = max(4, round(min(rgb.size) * fraction))
    blurred = rgb.filter(ImageFilter.GaussianBlur(radius))
    mean = ImageStat.Stat(rgb).mean
    src, low = rgb.load(), blurred.load()
    out = Image.new("RGB", rgb.size)
    dst = out.load()
    for y in range(rgb.height):
        for x in range(rgb.width):
            dst[x, y] = tuple(max(0, min(255, round(c * mean[i] / max(b, 1))))
                              for i, (c, b) in enumerate(zip(src[x, y], low[x, y])))
    return out


def trim(image, padding):
    """Ritaglia attorno alla parte non trasparente, con un margine."""
    box = image.getchannel("A").getbbox() if image.mode == "RGBA" else None
    if box is None:
        return image
    l, t, r, b = box
    return image.crop((max(0, l - padding), max(0, t - padding),
                       min(image.width, r + padding), min(image.height, b + padding)))


def resize(image, size):
    """Alla dimensione data: (larghezza, altezza), o un lato solo per il lato più lungo."""
    if isinstance(size, int):
        scale = size / max(image.width, image.height)
        size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    return image.resize(size, Image.Resampling.LANCZOS)


def darken(image, amount):
    """Scurisce verso il nero, per gli sfondi sotto il testo: amount 0 lascia com'è, 1 è nero."""
    rgb = image.convert("RGBA")
    black = Image.new("RGBA", rgb.size, (0, 0, 0, 255))
    out = Image.blend(rgb, black, amount)
    out.putalpha(rgb.getchannel("A"))
    return out


def finish(image, steps):
    """Applica i passi di una ricetta, in ordine: ("key", colore, low, high), ("trim", margine),
    ("resize", dimensione), ("darken", quanto), ("flatten",)."""
    for step in steps:
        kind, args = step[0], step[1:]
        if kind == "key":
            image = key_out(image, *args)
        elif kind == "trim":
            image = trim(image, *args)
        elif kind == "resize":
            image = resize(image, *args)
        elif kind == "darken":
            image = darken(image, *args)
        elif kind == "flatten":
            image = flatten(image, *args)
        else:
            raise ValueError(f"passo di finitura sconosciuto: {kind}")
    return image
