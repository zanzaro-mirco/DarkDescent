"""Pagina di ascolto dei suoni fatti da script: ogni suono con lo spettrogramma (0–8 kHz) e, in fondo
al riquadro, il suono più vicino che il gioco ha già. Tutto incorporato in un solo HTML, senza file
accanto (nella cartella Generated Unity importerebbe anche le immagini)."""
import base64
import html
import os
import struct
import zlib

import numpy as np
from scipy import signal
from scipy.io import wavfile

from sfxlib import SFX

PAGE = "ascolto.html"


def _png(rgb):
    h, w, _ = rgb.shape
    raw = b"".join(b"\x00" + rgb[y].astype(np.uint8).tobytes() for y in range(h))

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def _colormap(v):
    stops = np.array([[0, 0, 0], [60, 10, 90], [190, 50, 60], [250, 160, 30], [255, 250, 200]], float)
    v = np.clip(v, 0, 1) * (len(stops) - 1)
    i = np.minimum(v.astype(int), len(stops) - 2)
    f = (v - i)[..., None]
    return stops[i] * (1 - f) + stops[i + 1] * f


def spectrogram_png(path, width=600):
    if path.endswith(".ogg"):
        import soundfile
        x, sr = soundfile.read(path, dtype="float64")
    else:
        sr, d = wavfile.read(path)
        x = d.astype(np.float64) / 32768
    if x.ndim > 1:
        x = x.mean(axis=1)
    f, _, s = signal.spectrogram(x, sr, nperseg=1024, noverlap=896)
    db = 10 * np.log10(s[f <= 8000] + 1e-12)
    img = _colormap((db - (db.max() - 70)) / 70)[::-1]
    cols = np.linspace(0, img.shape[1] - 1, width).astype(int)
    return _png(np.repeat(img[:, cols], 2, axis=0))


def _data_uri(mime, data):
    return f"data:{mime};base64,{base64.b64encode(data).decode()}"


def _player(label, path):
    mime = "audio/ogg" if path.endswith(".ogg") else "audio/wav"
    with open(path, "rb") as f:
        src = _data_uri(mime, f.read())
    return f'<div class="p"><span>{html.escape(label)}</span><audio controls preload="auto" src="{src}"></audio></div>'


def write_page(out_dir, made, title="Ascolto suoni", note="Generati da Tools/Audio."):
    """made: [(modulo, [percorsi dei WAV o degli OGG])]. Restituisce il percorso della pagina."""
    cards = []
    for module, files in made:
        body = ""
        for path in files:
            name = os.path.splitext(os.path.basename(path))[0]
            body += _player(name, path)
            body += f'<img alt="spettrogramma" src="{_data_uri("image/png", spectrogram_png(path))}">'
        label, rel = module.COMPARE
        body += f'<div class="old">{_player("Oggi: " + label, os.path.join(SFX, rel))}</div>'
        cards.append(f"<section><h2>{html.escape(module.TITLE)}</h2>"
                     f'<p class="d">{html.escape(module.DESCRIPTION)}</p>{body}</section>')

    page = f"""<!doctype html><html lang="it"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>{html.escape(title)}</title>
<style>
:root {{ --bg:#141216; --card:#1f1b22; --ink:#ece6dc; --dim:#a39a8f; --line:#3a3340; --acc:#e0a050; }}
body {{ margin:0; background:var(--bg); color:var(--ink); font:15px/1.5 system-ui,sans-serif; }}
main {{ max-width:760px; margin:0 auto; padding:24px 16px 48px; }}
h1 {{ font-size:22px; margin:0 0 4px; }} .sub {{ color:var(--dim); margin:0 0 20px; }}
section {{ background:var(--card); border:1px solid var(--line); border-radius:10px; padding:16px; margin:0 0 16px; }}
h2 {{ font-size:17px; margin:0; color:var(--acc); }} .d {{ color:var(--dim); margin:4px 0 12px; }}
.p {{ display:flex; flex-direction:column; gap:4px; margin:10px 0 6px; }} .p span {{ font-size:13px; }}
audio {{ width:100%; }} img {{ width:100%; height:90px; object-fit:fill; border-radius:4px; display:block; }}
.old {{ border-top:1px dashed var(--line); margin-top:12px; padding-top:4px; }} .old span {{ color:var(--dim); }}
</style></head><body><main>
<h1>{html.escape(title)}</h1>
<p class="sub">{html.escape(note)} Sotto ogni suono lo spettrogramma (0–8 kHz); in fondo a ogni riquadro il suono più vicino che il gioco ha già.</p>
{"".join(cards)}
</main></body></html>"""
    path = os.path.join(out_dir, PAGE)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(page)
    return path
