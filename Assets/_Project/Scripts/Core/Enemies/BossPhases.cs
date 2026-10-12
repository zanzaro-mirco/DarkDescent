namespace DarkDescent.Enemies
{
    /// <summary>Quale fase del boss per la vita che gli resta (D8 della M10). Logica pura.</summary>
    public static class BossPhases
    {
        /// <summary>Sotto questa frazione della vita comincia a evocare.</summary>
        public const float SummonBelow = 0.6f;

        /// <summary>Sotto questa frazione della vita va in furia.</summary>
        public const float FuryBelow = 0.3f;

        public static BossPhase For(float current, float max)
        {
            float fraction = max > 0f ? current / max : 0f;
            if (fraction < FuryBelow)
            {
                return BossPhase.Fury;
            }

            return fraction < SummonBelow ? BossPhase.Summon : BossPhase.Melee;
        }
    }
}
