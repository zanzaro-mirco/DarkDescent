namespace DarkDescent.Stats
{
    /// <summary>
    /// Un modificatore su una statistica. Ricorda chi l'ha messo (un oggetto equipaggiato, più
    /// avanti un incantesimo): togliendo la sorgente se ne vanno tutti i suoi modificatori insieme.
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatType Stat;
        public readonly ModifierKind Kind;

        /// <summary>Per i fissi un valore assoluto, per le percentuali in punti: 10 vuol dire +10%.</summary>
        public readonly float Value;

        public readonly object Source;

        public StatModifier(StatType stat, ModifierKind kind, float value, object source)
        {
            Stat = stat;
            Kind = kind;
            Value = value;
            Source = source;
        }
    }
}
