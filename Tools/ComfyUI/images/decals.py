"""Le macchie a terra di cripta e caverne (M17, D5 e D16): otto immagini con la trasparenza, nate su un
fondo uniforme e ritagliate, che decalatlas.py mette in un atlante da 1024x512 (4 colonne, 2 righe, caselle
da 256). L'ordine di IMAGES è l'ordine delle caselle, lo stesso di DecalPlanner nel gioco. Negli asset
vanno con

    python Tools/ComfyUI/make.py decal_cracks decal_dust ... --out Tools/ComfyUI/out/images/decals
    python Tools/ComfyUI/decalatlas.py
"""

DESCRIPTION = ("Macchie a terra nello stile KayKit: colori piatti, dipinte a mano, viste dall'alto; rompono "
               "la ripetizione dei pavimenti senza cambiarne lo stile.")
COMPARE = "docs/media/m17/prima/09_crypt_room.jpg"

STYLE = ("isolated game decal seen straight from above, stylized hand painted low poly game art like KayKit, "
         "flat muted colors, soft edges, nothing else in the image, no floor tiles, no stone slabs, no text")

# il fondo da ritagliare: magenta, tranne per i rossi, che con il magenta se ne andrebbero (prove del 17.5)
MAGENTA = "on a flat pure magenta background"
GREEN = "on a flat pure bright green background"

# la ricetta comune: fondo via, ritaglio, casella da 256 e bordi sfumati
FINISH = [("key", "auto"), ("trim", 8), ("fit", 256), ("fade", 0.55)]


def decal(subject, seed, background=MAGENTA, extra=()):
    return {"prompt": f"{subject}, {STYLE}, {background}", "seed": seed, "size": (1024, 1024),
            "finish": FINISH + list(extra)}


IMAGES = {
    # solo le linee: con "crepe nella pietra" il modello disegna le lastre intere
    "decal_cracks": decal("A single long dark jagged fissure in the ground with small branching hairline cracks, "
                          "like a crack in dry earth, only the dark crack", 1782),
    # la polvere piena copre il pavimento: si tiene leggera
    "decal_dust": decal("A soft fuzzy smudge of pale grey powder, like spilled flour or ash, wispy cloudy edges", 1772,
                        extra=[("opacity", 0.55)]),
    "decal_bones": decal("Many tiny scattered bone fragments and small rib bones lying flat, seen from very far "
                         "above, small pieces spread out, no skull", 1765),
    "decal_blood": decal("An old dried blood stain, dark brownish red, irregular splatter shape", 1755, GREEN),
    "decal_moss": decal("A flat irregular patch of dark green moss with a few tiny leaves", 1755),
    "decal_puddle": decal("A small shallow puddle of dark murky water with a dull blue grey reflection", 1758),
    "decal_pebbles": decal("A handful of small grey brown pebbles and gravel scattered in a loose clump", 1757),
    "decal_soot": decal("A black soot scorch mark with charred edges, like where a fire burned", 1760),
}
