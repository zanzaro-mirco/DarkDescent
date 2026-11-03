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

        /// <summary>Il Dardo arcano a segno (Concentrazione, Scintilla, M21).</summary>
        BoltHit,

        /// <summary>Un nemico congelato (Mente di ghiaccio, M21).</summary>
        Freeze,
    }
}
