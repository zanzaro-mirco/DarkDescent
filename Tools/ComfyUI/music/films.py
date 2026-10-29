"""Ricette per ACE-Step 1.5: i brani dei due filmati (D10 della M19). L'apertura segue la notte che
cade sul borgo e l'alba dei sopravvissuti, 45–60 s; la vittoria accompagna il cavaliere che esce dalla
grotta all'alba, circa 30 s. Solo strumenti: il testo è [Instrumental]."""

TITLE = "I brani dei filmati (ACE-Step 1.5)"
DESCRIPTION = "L'apertura, L'ultima candela, e il secondo tempo della vittoria."
COMPARE = ("la musica della cripta", "../Music/crypt_dungeon002.ogg")
MUSIC = {
    "film_opening": {
        "tags": ("dark fantasy film score, slow and solemn, low strings, distant church bell, soft male "
                 "choir, a lonely cello melody, tension rising in the middle, quiet hopeful ending, "
                 "orchestral, cinematic, instrumental, no drums"),
        "bpm": 66,
        "keyscale": "D minor",
        "seconds": 60.0,
        "seed": 1904,
    },
    "film_victory": {
        "tags": ("dark fantasy film score, dawn after a long night, weary triumph, warm french horns, "
                 "strings swelling slowly, soft choir, bittersweet and hopeful, orchestral, cinematic, "
                 "instrumental"),
        "bpm": 72,
        "keyscale": "D major",
        "seconds": 30.0,
        "seed": 1905,
    },
}
