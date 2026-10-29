namespace DarkDescent.Films
{
    /// <summary>
    /// Il salto di un filmato (D9 della M19), come in Diablo: il primo Esc o click mostra in basso
    /// l'invito a saltare, il secondo, mentre l'invito c'è, salta. L'invito sparisce da solo dopo
    /// qualche secondo, e allora un tasto lo rimostra. Logica pura, con il tempo reale passato da fuori.
    /// </summary>
    public sealed class SkipPrompt
    {
        private readonly float _visibleFor;
        private float _shownAt = float.NegativeInfinity;

        /// <param name="visibleFor">Quanti secondi resta l'invito dopo il primo tasto.</param>
        public SkipPrompt(float visibleFor)
        {
            _visibleFor = visibleFor;
        }

        /// <summary>Il filmato va saltato.</summary>
        public bool Skipped { get; private set; }

        /// <summary>L'invito a saltare si vede al secondo <paramref name="now"/>.</summary>
        public bool IsShowing(float now) => !Skipped && now - _shownAt < _visibleFor;

        /// <summary>Esc o click al secondo <paramref name="now"/>: mostra l'invito, o salta se si vede già.</summary>
        public void Press(float now)
        {
            if (IsShowing(now))
            {
                Skipped = true;
                return;
            }

            _shownAt = now;
        }
    }
}
