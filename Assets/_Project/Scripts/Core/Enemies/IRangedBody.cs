namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quello che in più chiedono gli stati di un nemico a distanza (D6 della M9): quanto è lontano il
    /// bersaglio, se c'è il tiro, lanciare, avvicinarsi, fermarsi e allontanarsi.
    /// </summary>
    public interface IRangedBody : IEnemyBody
    {
        /// <summary>La distanza dal bersaglio, in pianta.</summary>
        float TargetDistance { get; }

        /// <summary>Un lancio è partito e il proiettile non è ancora uscito dalla mano.</summary>
        bool IsCasting { get; }

        /// <summary>La ricarica è finita: può lanciare.</summary>
        bool IsCastReady { get; }

        /// <summary>Niente muri tra lui e il bersaglio. Costa un raggio: si chiede di rado.</summary>
        bool HasClearShot();

        /// <summary>Lancia verso il bersaglio: si ferma, si gira e fa il gesto.</summary>
        void Cast();

        /// <summary>Va verso il bersaglio.</summary>
        void Approach();

        /// <summary>Resta dov'è, girato verso il bersaglio.</summary>
        void HoldPosition();

        /// <summary>Si avvia verso un punto lontano dal bersaglio; false se non ce n'è uno raggiungibile.</summary>
        bool TryRetreat();

        /// <summary>È arrivato dove stava andando, o non sta andando da nessuna parte.</summary>
        bool IsAtDestination { get; }
    }
}
