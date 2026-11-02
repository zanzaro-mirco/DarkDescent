namespace DarkDescent.Magic
{
    /// <summary>Se un incantesimo può partire, o perché no (D4 della M9: il posto si scurisce e suona sordo).</summary>
    public enum CastResult
    {
        Ready,
        NotKnown,
        NoMana,
        Cooldown,

        /// <summary>Nessun nemico da colpire, o troppo vicino o troppo lontano (le abilità del Guerriero, M20).</summary>
        NoTarget,

        /// <summary>Serve lo scudo in mano: Colpo di scudo e Muro di scudi (M20).</summary>
        NoShield,
    }
}
