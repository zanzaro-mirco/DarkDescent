namespace DarkDescent.Talents
{
    /// <summary>Il momento in cui scatta un effetto di un nodo (M20).</summary>
    public enum TalentTrigger
    {
        /// <summary>Un colpo d'arma andato a segno.</summary>
        Hit,

        /// <summary>Un nemico ucciso.</summary>
        Kill,

        /// <summary>Un colpo fermato dallo scudo.</summary>
        Block,
    }
}
