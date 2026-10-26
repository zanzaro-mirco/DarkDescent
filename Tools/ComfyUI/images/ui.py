"""Le immagini dei pannelli nello stile scuro e pulito (M17, D4 e lista del passo 17.1): lo sfondo che
si ripete e la cornice intera, che il passo 17.3 taglia in nove parti."""

DESCRIPTION = ("Pannelli nello stile scelto da Mirco: sfondi quasi neri, cornici sottili in metallo scuro, "
               "come i Diablo recenti.")
COMPARE = "docs/media/m17/prima/04_merchant.jpg"

STYLE = ("dark fantasy video game user interface material, modern minimal style like Diablo IV menus, "
         "very dark, almost black, subtle, low contrast, matte, no text, no logo")

IMAGES = {
    "panel_back": {
        # niente "seamless" né "tileable": il modello disegnerebbe lui una griglia di lastre, con una
        # linea al centro; alla ripetizione pensano lo spostamento e la croce ridisegnata
        "prompt": ("Extreme close-up macro photograph of a single sheet of very dark charcoal leather that "
                   "fills the whole frame edge to edge, uniform fine grain, a few barely visible hairline "
                   "scratches, flat even diffuse lighting, seen straight from above, " + STYLE),
        "seed": 1701,
        "size": (1024, 1024),
        "flatten": True,
        "tile": True,
        "finish": [("resize", 512)],
    },
    "panel_frame": {
        "prompt": ("A thin empty rectangular frame border made of dark blackened iron, square format, "
                   "small gothic filigree flourishes only at the four corners, straight thin sides, a tiny "
                   "dull gold inlay line, seen straight from the front, flat lighting, the inside of the frame "
                   "and the outside are the same flat pure magenta background, " + STYLE),
        "seed": 1711,
        "size": (1024, 1024),
        "finish": [("key", "auto"), ("trim", 2), ("resize", 512)],
    },
}
