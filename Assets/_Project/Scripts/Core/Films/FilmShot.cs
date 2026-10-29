namespace DarkDescent.Films
{
    /// <summary>
    /// Un'inquadratura di un filmato (D2 della M19): da quando a quando, quale camera la riprende e
    /// la didascalia che resta sotto mentre dura, se c'è. I tempi sono in secondi dall'inizio.
    /// </summary>
    public sealed class FilmShot
    {
        public FilmShot(float start, float duration, string camera, string captionKey = null)
        {
            Start = start;
            Duration = duration;
            Camera = camera;
            CaptionKey = captionKey;
        }

        public float Start { get; }

        public float Duration { get; }

        public float End => Start + Duration;

        /// <summary>Il nome della camera nel set, quello che lo strumento dei filmati le dà.</summary>
        public string Camera { get; }

        /// <summary>La chiave della didascalia nella tabella delle stringhe; null se l'inquadratura non ne ha.</summary>
        public string CaptionKey { get; }

        public bool Contains(float time) => time >= Start && time < End;
    }
}
