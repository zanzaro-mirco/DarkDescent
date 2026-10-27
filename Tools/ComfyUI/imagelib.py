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


def fade(image, start=0.6):
    """Sfuma la trasparenza verso i bordi, per le macchie a terra: dentro un cerchio di raggio start
    (in frazioni di mezzo lato) resta com'è, poi scende fino a zero sul bordo. Niente contorni netti
    sul pavimento, e niente righe dove la macchia tocca il bordo del suo riquadro nell'atlante."""
    rgba = image.convert("RGBA")
    w, h = rgba.size
    alpha = rgba.getchannel("A")
    src = alpha.load()
    cx, cy = (w - 1) / 2, (h - 1) / 2
    for y in range(h):
        for x in range(w):
            r = (((x - cx) / (w / 2)) ** 2 + ((y - cy) / (h / 2)) ** 2) ** 0.5
            if r > start:
                k = max(0.0, 1.0 - (r - start) / (1.0 - start)) if start < 1 else 0.0
                src[x, y] = round(src[x, y] * k * k * (3 - 2 * k))
    rgba.putalpha(alpha)
    return rgba


def opacity(image, amount):
    """Moltiplica la trasparenza: le macchie leggere (polvere) si vedono appena sul pavimento."""
    rgba = image.convert("RGBA")
    rgba.putalpha(rgba.getchannel("A").point(lambda a: round(a * amount)))
    return rgba


def fit(image, size):
    """Un quadrato di size pixel con l'immagine al centro, rimpicciolita sul lato più lungo: le
    caselle dell'atlante delle macchie sono tutte uguali."""
    rgba = resize(image.convert("RGBA"), size)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(rgba, ((size - rgba.width) // 2, (size - rgba.height) // 2))
    return out


def atlas(images, columns, cell):
    """Le immagini in una griglia di columns colonne, caselle di cell pixel, riga per riga: la casella
    i sta alla colonna i % columns e alla riga i // columns, dall'alto."""
    rows = (len(images) + columns - 1) // columns
    out = Image.new("RGBA", (columns * cell, rows * cell), (0, 0, 0, 0))
    for i, image in enumerate(images):
        out.paste(fit(image, cell), ((i % columns) * cell, (i // columns) * cell))
    return out


def nine_slice(image, corner, middle=16):
    """Una cornice per le nove parti: tiene i quattro angoli (corner pixel) e un tratto di lato largo
    middle, togliendo il centro dei lati. Le decorazioni a metà lato, che allungate diventerebbero
    strisce, se ne vanno; il bordo dello sprite è corner su ogni lato."""
    w, h = image.size
    keep = corner + middle // 2
    if 2 * keep >= min(w, h):
        raise ValueError(f"l'angolo di {corner} pixel non sta in un'immagine di {w}x{h}")
    out = Image.new(image.mode, (2 * keep, 2 * keep))
    for sx, dx in ((0, 0), (w - keep, keep)):
        for sy, dy in ((0, 0), (h - keep, keep)):
            out.paste(image.crop((sx, sy, sx + keep, sy + keep)), (dx, dy))
    return out


def button_plate(back, size, border):
    """La placca dei pulsanti, dallo sfondo dei pannelli: un ritaglio un po' più chiaro, con un filo
    scuro fuori e un filo d'oro spento dentro. È grigia: il colore lo mette il pulsante, più chiaro al
    passaggio del mouse."""
    rgb = back.convert("RGB")
    crop = rgb.crop((0, 0, min(rgb.width, size * 2), min(rgb.height, size * 2))).resize((size, size))
    plate = Image.blend(crop, Image.new("RGB", (size, size), (110, 104, 96)), 0.35).convert("RGBA")
    draw = ImageDraw.Draw(plate)
    draw.rectangle((0, 0, size - 1, size - 1), outline=(18, 16, 14, 255), width=1)
    draw.rectangle((1, 1, size - 2, size - 2), outline=(150, 122, 70, 255), width=1)
    draw.rectangle((2, 2, size - 3, size - 3), outline=(40, 34, 28, 255), width=max(1, border - 2))
    return plate


def finish(image, steps):
    """Applica i passi di una ricetta, in ordine: ("key", colore, low, high), ("trim", margine),
    ("resize", dimensione), ("darken", quanto), ("flatten",), ("nineslice", angolo, tratto),
    ("fade", inizio), ("fit", lato), ("opacity", quanto)."""
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
        elif kind == "nineslice":
            image = nine_slice(image, *args)
        elif kind == "fade":
            image = fade(image, *args)
        elif kind == "fit":
            image = fit(image, *args)
        elif kind == "opacity":
            image = opacity(image, *args)
        else:
            raise ValueError(f"passo di finitura sconosciuto: {kind}")
    return image
