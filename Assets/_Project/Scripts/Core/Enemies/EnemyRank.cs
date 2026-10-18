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
    }
}
