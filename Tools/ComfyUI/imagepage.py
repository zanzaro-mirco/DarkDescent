"""La pagina di confronto delle immagini generate (M17, D10): per ogni ricetta la foto di prima e i
semi provati, ripetuti due volte per lato se si ripetono e su una scacchiera se sono trasparenti."""
import html
import os

REPO = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

STYLE = """
:root { --bg:#14110e; --panel:#221c16; --line:#4a3d2c; --text:#e8e2d4; --dim:#9c927f; --gold:#d9b25c; --bad:#c0473a; }
body { margin:0; background:var(--bg); color:var(--text); font:15px/1.45 system-ui, sans-serif; }
header, section { padding:16px 24px; }
h1 { margin:0 0 6px; font-size:22px; color:var(--gold); } h2 { font-size:18px; color:var(--gold); margin:0 0 4px; }
p { margin:0 0 10px; color:var(--dim); max-width:900px; }
.row { display:flex; gap:14px; flex-wrap:wrap; align-items:flex-start; }
figure { margin:0; background:var(--panel); border:1px solid var(--line); border-radius:6px; padding:8px; }
figure img, .tile { display:block; width:360px; max-width:100%; }
.tile { height:360px; background-size:180px 180px; background-repeat:repeat; }
.alpha { background-image:linear-gradient(45deg,#3a3a3a 25%,transparent 25%,transparent 75%,#3a3a3a 75%),
  linear-gradient(45deg,#3a3a3a 25%,#2a2a2a 25%,#2a2a2a 75%,#3a3a3a 75%); background-size:20px 20px; background-position:0 0,10px 10px; }
figcaption { font-size:13px; color:var(--dim); margin-top:6px; }
.bad { color:var(--bad); }
@media (max-width:520px) { header, section { padding-left:16px; padding-right:16px; } }
"""


def _rel(out_dir, path):
    return os.path.relpath(path, out_dir).replace(os.sep, "/")


def write_page(out_dir, made, limit):
    """made: [(modulo, nome, ricetta, [(file, seme, cuciture)])]; scrive index.html in out_dir."""
    parts = [f"<!doctype html><html lang='it'><head><meta charset='utf-8'>"
             f"<meta name='viewport' content='width=device-width, initial-scale=1'><title>Immagini generate</title>"
             f"<style>{STYLE}</style></head><body><header><h1>Immagini generate</h1>"
             f"<p>Ogni riga è una ricetta: a sinistra la foto di prima, poi i semi provati. Le immagini che si "
             f"ripetono sono mostrate ripetute; quelle trasparenti su una scacchiera. Le cuciture sopra "
             f"{limit} si vedono.</p></header>"]
    for module, name, recipe, takes in made:
        parts.append(f"<section><h2>{html.escape(name)}</h2><p>{html.escape(getattr(module, 'DESCRIPTION', ''))}</p>"
                     f"<p>{html.escape(recipe['prompt'])}</p><div class='row'>")
        compare = getattr(module, "COMPARE", None)
        if compare:
            src = _rel(out_dir, os.path.join(REPO, compare))
            parts.append(f"<figure><img src='{html.escape(src)}' alt='prima'><figcaption>prima: "
                         f"{html.escape(compare)}</figcaption></figure>")
        for path, seed, score in takes:
            src = html.escape(_rel(out_dir, path))
            alpha = " alpha" if any(step[0] == "key" for step in recipe.get("finish", [])) else ""
            if recipe.get("tile"):
                body = f"<div class='tile{alpha}' style=\"background-image:url('{src}')\"></div>"
            else:
                body = f"<div class='{alpha.strip()}'><img src='{src}' alt='{seed}'></div>"
            seam = ""
            if score is not None:
                seam = f" · cuciture <span class='{'bad' if score > limit else ''}'>{score:.2f}</span>"
            parts.append(f"<figure>{body}<figcaption>seme {seed}{seam} · {html.escape(os.path.basename(path))}"
                         f"</figcaption></figure>")
        parts.append("</div></section>")
    parts.append("</body></html>")
    page = os.path.join(out_dir, "index.html")
    with open(page, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(parts))
    return page
