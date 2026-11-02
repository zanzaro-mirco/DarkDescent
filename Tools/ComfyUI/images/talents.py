"""Le icone delle abilità e dei talenti del Guerriero (M20, D12): una per ognuna delle undici abilità attive e
una per ogni tipo di passivo (danno, vita, critico, armatura, blocco…). Sono quadrate e a tutta pagina: dentro
i nodi tondi e ottagonali le ritaglia la placca, e nelle caselle della barra le ritaglia la cornice. I semi
sono quelli scelti al passo 20.7 tra tre provati per icona; per rifarle:

    python Tools/ComfyUI/make.py --takes 1 --out Assets/_Project/Art/Icons/Talents talents
"""

DESCRIPTION = ("Le icone del Guerriero nello stile di Diablo III: pittura scura e contrastata, un soggetto solo "
               "al centro con un punto luminoso. Le abilità attive in alto, poi i tipi di passivo.")
COMPARE = "Recordings/Photos/pannello/talents_tree_16x9.jpg"

STYLE = ("game ability icon in the style of Diablo III, hand painted digital illustration, dark fantasy, dramatic "
         "lighting, high contrast, rich colors with one glowing focal point, a single centered subject that fills "
         "the square frame, dark moody background with a soft vignette, no text, no letters, no border, no frame")

_FINISH = [("resize", 256)]


def _icon(subject, seed):
    return {"prompt": subject + ", " + STYLE, "seed": seed, "size": (1024, 1024), "finish": _FINISH}


# (nome del PNG, soggetto, seme scelto tra i tre provati del passo 20.7)
_ICONS = (
    # le abilità attive
    ("skill_power_strike", "a mighty broadsword slash cutting the air, a bright orange arc of fire and sparks", 2201),
    ("skill_charge", "an armored knight charging forward at full speed with a streak of blue motion lines and a cloud of dust", 2204),
    ("skill_stone_skin", "a muscular arm turning into grey cracked granite stone, solid rock texture", 2209),
    ("skill_battle_shout", "a roaring warrior with his mouth wide open, red sound waves bursting outward", 2214),
    ("skill_shield_bash", "a heavy round iron shield smashing forward with a white impact shockwave", 2216),
    ("skill_shield_wall", "a tall steel tower shield with a golden glowing barrier rising around it", 2221),
    ("skill_last_stand", "a lone wounded knight kneeling with his sword planted in the ground, a golden beam of light from above", 2224),
    ("skill_whirlwind", "a spinning vortex of steel blades around a warrior, a swirling whirlwind of sparks", 2228),
    ("skill_execute", "a heavy executioner's axe falling on a skull, a red glow and splash of blood", 2234),
    ("skill_leap", "a warrior leaping high into the air with his sword raised, the ground cracking below him", 2236),
    ("skill_bladestorm", "a storm of many flying spinning swords forming a deadly steel tornado", 2242),
    # i tipi di passivo
    ("talent_damage", "a gloved fist gripping a sword hilt, red glowing power", 2300),
    ("talent_life", "a glowing red heart with a soft crimson aura", 2305),
    ("talent_crit", "a blade striking with a bright white starburst flash, a critical hit", 2308),
    ("talent_accuracy", "an eye with a golden target reticle over a sword tip", 2312),
    ("talent_cooldown", "an old clock face with swirling golden arrows, time running faster", 2316),
    ("talent_armor", "a sturdy steel breastplate with bolts and a gold trim, shining", 2320),
    ("talent_mana_hit", "a sword strike releasing blue mana sparks that flow into a crystal", 2324),
    ("talent_skin", "a hand and forearm with a shimmering hard stone-like skin, grey and bronze", 2329),
    ("talent_lifesteal", "a drop of blood dripping from a sword blade and turning into a red glow", 2333),
    ("talent_attack_speed", "a sword drawn with many ghostly afterimages showing very fast swings", 2336),
    ("talent_stun", "a dented helmet with golden stars circling above it, dazed", 2340),
    ("talent_last_defense", "a cracked red heart protected by a glowing silver shield", 2345),
    ("talent_speed", "a leather boot with golden wings, fast movement", 2348),
    ("talent_potion", "a corked glass flask with a glowing red healing potion", 2352),
    ("talent_weapon_master", "two crossed swords above a golden laurel wreath", 2356),
    ("talent_area", "concentric shockwave rings expanding on the ground from a point of impact", 2360),
    ("talent_heal", "a glowing green cross made of light with healing sparkles", 2364),
    ("talent_iron_heart", "a heart made of dark riveted iron with a faint red glow inside", 2368),
    ("talent_block", "a shield deflecting a sword slash with a burst of sparks", 2372),
    ("talent_thick_skin", "a scarred tough leathery hide with stitched patches, brown and warm", 2376),
    ("talent_recoil", "a shield bouncing back an attack, blue sparks flying off it", 2380),
    ("talent_shield_armor", "a knight shield with a heavy plated rim and a gold boss, shining", 2384),
    ("talent_reflect", "a mirror-polished shield reflecting a red beam back at the attacker", 2388),
    ("talent_avenger", "a glowing sword raised high behind a shield, a burst of golden vengeance light", 2393),
    ("talent_resist", "a translucent glowing dome of protective energy over a small figure", 2397),
    ("talent_fortress", "a massive stone fortress tower with battlements against a stormy sky", 2401),
    ("talent_two_handed", "a huge greatsword held by two gauntleted hands, planted point down", 2405),
    ("talent_crit_damage", "a skull split by a red lightning crack, a devastating blow", 2409),
    ("talent_mana_kill", "a skull releasing blue wisps of mana upward", 2413),
    ("talent_threshold", "a wounded skull with a red life bar half empty, ready to be finished", 2417),
    ("talent_mana_cost", "a blue mana crystal with a cracked facet, glowing, cheaper power", 2420),
    ("talent_frenzy", "a snarling berserker face with a red fiery aura and glowing eyes", 2425),
    ("talent_duration", "an hourglass with golden glowing sand, time that lasts longer", 2428),
)

IMAGES = {name: _icon(subject, seed) for name, subject, seed in _ICONS}
