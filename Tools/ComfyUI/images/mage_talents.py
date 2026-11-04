"""Le icone delle abilità e dei talenti del Mago (M21, D13): una per ognuna delle dodici abilità attive (il Dardo
arcano del sinistro e le undici degli alberi) e una per ogni tipo di passivo nuovo (danno degli incantesimi, mana,
bruciatura, gelo…). I tipi che il Guerriero ha già (cooldown, area, vita, armatura, durata…) si riusano. Sono nello
stesso stile e nella stessa dimensione di quelle della M20 (`talents.py`): quadrate e a tutta pagina, le ritaglia la
placca dei nodi o la cornice delle caselle. I semi sono quelli scelti al passo 21.7 tra tre provati per icona; per
rifarle:

    python Tools/ComfyUI/make.py --takes 1 --out Assets/_Project/Art/Icons/Talents mage_talents
"""

DESCRIPTION = ("Le icone del Mago nello stile di Diablo III, come quelle del Guerriero: pittura scura e contrastata, "
               "un soggetto solo al centro con un punto luminoso. Le abilità attive in alto, poi i tipi di passivo nuovi.")
COMPARE = "Recordings/Photos/mago_albero/mage_tree.jpg"

STYLE = ("game ability icon in the style of Diablo III, hand painted digital illustration, dark fantasy, dramatic "
         "lighting, high contrast, rich colors with one glowing focal point, a single centered subject that fills "
         "the square frame, dark moody background with a soft vignette, no text, no letters, no border, no frame")

_FINISH = [("resize", 256)]


def _icon(subject, seed):
    return {"prompt": subject + ", " + STYLE, "seed": seed, "size": (1024, 1024), "finish": _FINISH}


# (nome del PNG, soggetto, seme scelto tra i tre provati del passo 21.7)
_ICONS = (
    # le abilità attive
    ("skill_arcane_bolt", "a small bolt of violet arcane energy shooting from a wizard's wand, glowing purple sparks", 2501),
    ("skill_firebolt", "a flaming fire bolt streaking forward with a long bright orange tail of embers", 2504),
    ("skill_fire_ring", "a ring of roaring flames expanding outward on the ground around a point, a circle of fire", 2507),
    ("skill_blink", "a wizard dissolving into a trail of bright blue light, teleporting, a swirl of glowing blue particles", 2510),
    ("skill_mana_shield", "a translucent glowing blue magical sphere shield around a swirling mana crystal", 2514),
    ("skill_fireball", "a huge blazing fireball exploding in a burst of orange and yellow flames", 2516),
    ("skill_flame_wall", "a tall wall of fire rising from the ground in a line, orange flames and black smoke", 2520),
    ("skill_meteor", "a giant flaming meteor rock falling from the dark sky trailing fire, about to crash", 2524),
    ("skill_ice_bolt", "a sharp ice crystal shard flying forward with a trail of frost, pale blue glow", 2527),
    ("skill_frost_nova", "a burst of ice crystals exploding outward in a ring from the center, frost shockwave, cyan glow", 2528),
    ("skill_frost_armor", "a figure covered in a shimmering armor of ice plates and frost, pale blue glow", 2532),
    ("skill_blizzard", "a swirling spiral of snow and many sharp flying ice shards forming a freezing whirlwind against a dark stormy sky, white and cyan, no symbols", 2541),
    # i tipi di passivo nuovi
    ("talent_spell_damage", "a glowing arcane rune star bursting with raw magical power, violet and white light", 2600),
    ("talent_mana_pool", "a large blue crystal flask brimming with glowing mana liquid", 2604),
    ("talent_mana_regen", "a blue mana crystal surrounded by swirling circular arrows of light, restoring", 2609),
    ("talent_fire_damage", "a roaring flame with a bright white-hot core, intense heat, red and orange", 2613),
    ("talent_frost_damage", "a jagged ice shard piercing forward with a white-blue glow, cold damage", 2616),
    ("talent_magic", "a glowing blue arcane sigil circle with floating runes, magical energy", 2621),
    ("talent_slow", "a snowflake wrapped with heavy icy chains, slowing frost, pale blue", 2625),
    ("talent_freeze", "a block of clear ice with a small figure frozen inside, frozen solid, cyan glow", 2629),
    ("talent_free_cast", "a wand shooting a bolt with a golden free spark, a ghostly second bolt, no cost", 2633),
    ("talent_burn", "a dark humanoid silhouette engulfed in bright orange flames, a burning figure with fire licking up its body", 2661),
    ("talent_spark_rain", "a shower of falling bright orange sparks and embers raining down, sparks everywhere", 2641),
    ("talent_echo", "a glowing spell rune repeated twice with a faint ghostly copy behind it, an echo", 2644),
    ("talent_fire_ground", "burning ground with cracks of lava and low flames spreading on the floor", 2649),
    ("talent_cast_haste", "a pair of hands casting spells very fast with blurred glowing afterimages, blue speed lines", 2652),
)

IMAGES = {name: _icon(subject, seed) for name, subject, seed in _ICONS}
