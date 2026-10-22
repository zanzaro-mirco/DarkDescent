namespace DarkDescent.Enemies
{
    /// <summary>Quanto è speciale un nemico (M12): normale, campione o unico.</summary>
    public enum EnemyRank
    {
        Normal,

        /// <summary>Un nemico normale reso più forte, con uno o due modificatori e una scorta (D2 della M12).</summary>
        Champion,

        /// <summary>Un nemico con un nome suo e un aspetto suo, uno per tipo di livello (D6 della M12).</summary>
        Unique,

        /// <summary>
        /// Il re alle difficoltà alte (D5 della M14): i modificatori dei campioni, ma niente numeri in
        /// più, tinta o anello. Vita e danno li alza già la difficoltà.
        /// </summary>
        Boss,
    }
}
