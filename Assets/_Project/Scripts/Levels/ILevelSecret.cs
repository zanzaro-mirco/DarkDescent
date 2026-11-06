namespace DarkDescent.Levels
{
    /// <summary>
    /// Un segreto che il gioco mette sopra il livello generato al caricamento (la stanza sigillata con la sua leva, M25): si risolve una volta e il
    /// ricordo del livello (ADR-079) lo sa. L'ordine in <see cref="LevelContext.Secrets"/> è lo stesso a ogni caricamento dello stesso livello, perché
    /// nasce dalla mappa e dal seme.
    /// </summary>
    public interface ILevelSecret
    {
        /// <summary>Il segreto è già stato risolto.</summary>
        bool IsDone { get; }

        /// <summary>Lo rimette com'era dopo essere stato risolto in una visita di prima.</summary>
        void RestoreDone();
    }
}
