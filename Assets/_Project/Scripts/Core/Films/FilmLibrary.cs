namespace DarkDescent.Films
{
    /// <summary>
    /// I filmati del gioco (D7 e D8 della M19), con le loro inquadrature. L'apertura racconta in cinque
    /// scene la storia dell'ultima candela: il borgo di notte, il cimitero, l'alba, l'altare, il titolo.
    /// La vittoria è in tre pezzi, perché passa per due livelli: il re che cade, nella sala; il cavaliere
    /// che guarda le sue ossa ed entra nel portale, nella sala; il ritorno nel borgo all'alba, dopo il
    /// viaggio. I nomi delle camere sono quelli che il set del filmato sa muovere.
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

        public const string VictoryFallId = "VictoryFall";
        public const string VictoryLeaveId = "VictoryLeave";
        public const string VictoryReturnId = "VictoryReturn";

        public const string FallCamera = "fall";
        public const string BonesCamera = "bones";
        public const string TunnelCamera = "tunnel";
        public const string SquareCamera = "square";
        public const string HomeCamera = "home";

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

        /// <summary>Il primo tempo della vittoria, appena muore il re: il colpo rallenta e il re si sfalda.</summary>
        public static FilmScript VictoryFall { get; } = new FilmScript(VictoryFallId, "film.victory.name", 3f, 8f, new[]
        {
            new FilmShot(0f, 5f, FallCamera),
        });

        /// <summary>Il secondo tempo nella sala, al portale: il cavaliere guarda le ossa del re ed entra.</summary>
        public static FilmScript VictoryLeave { get; } = new FilmScript(VictoryLeaveId, "film.victory.name", 3f, 10f, new[]
        {
            new FilmShot(0f, 5.5f, BonesCamera, "film.victory.fallen"),
        });

        /// <summary>Il secondo tempo nel borgo, all'alba: il cavaliere esce dalla grotta e i sopravvissuti esultano.</summary>
        public static FilmScript VictoryReturn { get; } = new FilmScript(VictoryReturnId, "film.victory.name", 10f, 25f, new[]
        {
            new FilmShot(0f, 5f, TunnelCamera, "film.victory.dawn"),
            new FilmShot(5f, 3f, SquareCamera),
            new FilmShot(8f, 6f, SquareCamera, "film.victory.candles"),
            new FilmShot(14f, 3f, HomeCamera),
        });
    }
}
