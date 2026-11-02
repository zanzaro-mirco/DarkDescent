"""La barra in basso alla Diablo 3 (M20, D18): la console tra le due sfere, la sua cornice e le caselle
delle abilità e delle pozioni. I semi sono quelli scelti sulla pagina del passo 20.4b; negli asset vanno con

    python Tools/ComfyUI/make.py hud_back hud_frame skill_frame potion_frame --out Assets/_Project/Art/Generated/UI
"""

DESCRIPTION = ("La barra delle abilità e la cintura nello stile di Diablo 3: pietra scura scolpita, ferro "
               "annerito e bordi d'oro antico, più ricca dei pannelli ma della stessa famiglia.")
COMPARE = "Recordings/Photos/09_crypt_room.jpg"

STYLE = ("dark fantasy action RPG game user interface, in the style of Diablo III, hand painted, "
         "rich detail, dark, weathered, no text, no logo, no letters")

IMAGES = {
    "hud_back": {
        # come lo sfondo dei pannelli: un materiale uniforme, che si ripete sfumando la copia spostata
        "prompt": ("Extreme close-up photograph of a flat slab of dark carved basalt stone that fills the "
                   "whole frame edge to edge, faint worn gothic engravings, tiny cracks, dark grey with a "
                   "warm brown tint, flat even lighting, seen straight from above, " + STYLE),
        "seed": 2042,
        "size": (1024, 1024),
        "flatten": True,
        "tile": True,
        "finish": [("darken", 0.25), ("resize", 512)],
    },
    "hud_frame": {
        "prompt": ("A heavy empty rectangular frame border made of blackened wrought iron with antique gold "
                   "trim, square format, ornate gothic corner pieces with small skulls and spikes, straight "
                   "sides with rivets, seen straight from the front, flat lighting, the inside of the frame "
                   "and the outside are the same flat pure magenta background, " + STYLE),
        "seed": 2052,
        "size": (1024, 1024),
        "finish": [("key", "auto"), ("trim", 2), ("resize", 512), ("nineslice", 84, 16)],
    },
    "skill_frame": {
        "prompt": ("A single square socket frame for a skill icon, thick beveled antique gold and bronze rim "
                   "with fine engraved filigree, dark iron inner edge, four small ornaments at the corners, "
                   "seen straight from the front, the empty square inside and the outside are the same flat "
                   "pure magenta background, " + STYLE),
        "seed": 2063,
        "size": (1024, 1024),
        "finish": [("key", "auto"), ("trim", 2), ("resize", 160)],
    },
    "potion_frame": {
        "prompt": ("A single small square socket frame for a potion, thick blackened iron rim with dull silver "
                   "edges and four round rivets, simple and sturdy, seen straight from the front, the empty "
                   "square inside and the outside are the same flat pure magenta background, " + STYLE),
        "seed": 2071,
        "size": (1024, 1024),
        "finish": [("key", "auto"), ("trim", 2), ("resize", 128)],
    },
}
