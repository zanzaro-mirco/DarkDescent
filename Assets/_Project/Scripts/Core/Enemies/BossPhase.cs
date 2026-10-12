namespace DarkDescent.Enemies
{
    /// <summary>Le tre fasi del boss (D8 della M10), dalla vita che gli resta.</summary>
    public enum BossPhase
    {
        /// <summary>Sopra il 60%: mischia e colpo caricato, come il bruto.</summary>
        Melee,

        /// <summary>Tra il 60 e il 30%: evoca un gruppo dello sciame a intervalli.</summary>
        Summon,

        /// <summary>Sotto il 30%: più veloce, e lancia l'anello di fuoco telegrafato.</summary>
        Fury,
    }
}
