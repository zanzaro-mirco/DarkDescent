"""Ricette per Stable Audio 3 Small SFX: i suoni dei filmati (D10 della M19). Le candele del borgo che si
spengono una a una e la terra che si rompe sopra gli scheletri nell'apertura; i sopravvissuti che
esultano attorno al fuoco nel ritorno della vittoria."""

TITLE = "I suoni dei filmati (Stable Audio)"
DESCRIPTION = "La candela che si spegne, la terra che si rompe e il borgo che esulta, per l'apertura e la vittoria."
COMPARE = None
MAX_DUR = 6.0
SOUNDS = {
    "candle_snuff_sa": {
        "prompt": ("A single candle flame blown out by a soft gust of night wind, short airy puff and a tiny "
                   "fading hiss of the wick, quiet, close, dry room"),
        "negative": "music, melody, speech, voice, fire roar, explosion, birds, low quality, distorted",
        "seconds": 1.5,
        "seed": 1961,
    },
    "earth_break_sa": {
        "prompt": ("Hard graveyard soil cracking and breaking open from below, clods of dirt and small stones "
                   "crumbling and sliding, dry roots snapping, bones scraping, outdoor at night, close"),
        "negative": "music, melody, speech, voice, rain, thunder, footsteps, low quality, distorted",
        "seconds": 3.0,
        "seed": 1971,
    },
    "crowd_cheer_sa": {
        "prompt": ("A small group of five or six tired medieval villagers cheering and shouting with joy "
                   "outdoors at dawn, men and women, clapping hands, some laughter, a crackling campfire "
                   "nearby, no words"),
        "negative": "music, melody, stadium, large crowd, applause of an audience, announcer, low quality, distorted",
        "seconds": 6.0,
        "seed": 1981,
    },
}
