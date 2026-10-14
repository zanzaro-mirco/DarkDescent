namespace DarkDescent.Magic
{
    /// <summary>Se la magia del portale parte, o perché no (passo 11.7 della M11).</summary>
    public enum PortalResult
    {
        Opened,

        /// <summary>In città il portale non serve.</summary>
        InTown,

        /// <summary>Nella sala del re, vivo: dallo scontro non si fugge.</summary>
        BossRoom,

        NoMana,

        /// <summary>Morto, in un cambio di livello, o senza chi apre il portale: niente, senza avvisi.</summary>
        Unavailable,
    }
}
