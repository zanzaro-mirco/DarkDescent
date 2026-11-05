namespace DarkDescent.Enemies
{
    /// <summary>
    /// Gli interruttori dei comportamenti nuovi dei nemici (M24): accesi nel gioco, spenti dai test PlayMode, che contano su nemici
    /// che aspettano fermi e non scappano (D17 della M24), come i suggerimenti e le barre della M23. Solo i test del comportamento
    /// li accendono.
    /// </summary>
    public static class EnemyBehaviours
    {
        /// <summary>Gli scheletri e lo sciame scappano a vita bassa o per i compagni morti (D2–D5).</summary>
        public static bool FleeEnabled { get; set; } = true;
    }
}
