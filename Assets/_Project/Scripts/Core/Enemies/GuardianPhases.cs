namespace DarkDescent.Enemies
{
    /// <summary>Le due fasi di Vorrak il Custode (D8 della M24).</summary>
    public enum GuardianPhase
    {
        /// <summary>Sopra la metà della vita: la mischia con il colpo caricato, largo e forte.</summary>
        Strike,

        /// <summary>Sotto la metà: evoca tre scheletri, una sola volta, e carica in linea retta contro il cavaliere.</summary>
        Wrath,
    }

    /// <summary>Quale fase del Custode per la vita che gli resta. Logica pura, come <see cref="BossPhases"/>.</summary>
    public static class GuardianPhases
    {
        /// <summary>Sotto questa frazione della vita comincia la seconda fase.</summary>
        public const float WrathBelow = 0.5f;

        public static GuardianPhase For(float current, float max)
        {
            float fraction = max > 0f ? current / max : 0f;
            return fraction < WrathBelow ? GuardianPhase.Wrath : GuardianPhase.Strike;
        }
    }
}
