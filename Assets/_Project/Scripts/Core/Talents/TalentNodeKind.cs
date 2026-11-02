namespace DarkDescent.Talents
{
    /// <summary>Il tipo di un nodo, come nel pannello (D10 della M20): quadrato, cerchio, ottagono.</summary>
    public enum TalentNodeKind
    {
        /// <summary>Insegna un'abilità, che va nella barra.</summary>
        Active,

        /// <summary>Cambia statistiche o abilità, senza tasti.</summary>
        Passive,

        /// <summary>Due opzioni, se ne prende una.</summary>
        Choice,
    }
}
