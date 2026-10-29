namespace DarkDescent.Films
{
    /// <summary>
    /// I filmati del gioco (D7 e D8 della M19), con le loro inquadrature. L'apertura racconta in cinque
    /// scene la storia dell'ultima candela: il borgo di notte, il cimitero, l'alba, l'altare, il titolo.
    /// I nomi delle camere sono quelli che il set del filmato sa muovere.
    /// </summary>
    public static class FilmLibrary
    {
        public const string OpeningId = "Opening";

        public const string NightCamera = "night";
        public const string GravesCamera = "graves";
        public const string DawnCamera = "dawn";
        public const string ArrivalCamera = "arrival";
        public const string AltarCamera = "altar";
        public const string TitleCamera = "title";
        public const string DescentCamera = "descent";

        /// <summary>L'apertura, a ogni partita nuova dal menu: circa 50 s.</summary>
        public static FilmScript Opening { get; } = new FilmScript(OpeningId, "film.opening.name", 40f, 60f, new[]
        {
            new FilmShot(0f, 6f, NightCamera, "film.opening.vigil"),
            new FilmShot(6f, 5f, NightCamera, "film.opening.out"),
            new FilmShot(11f, 11f, GravesCamera, "film.opening.rise"),
            new FilmShot(22f, 8f, DawnCamera, "film.opening.dawn"),
            new FilmShot(30f, 5f, ArrivalCamera, "film.opening.knight"),
            new FilmShot(35f, 7f, AltarCamera, "film.opening.king"),
            new FilmShot(42f, 4.5f, TitleCamera, null, "film.opening.title"),
            new FilmShot(46.5f, 3.5f, DescentCamera),
        });
    }
}
