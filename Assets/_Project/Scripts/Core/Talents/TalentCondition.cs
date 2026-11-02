namespace DarkDescent.Talents
{
    /// <summary>
    /// Quando vale un effetto di un nodo (M20). Scudo e due mani dipendono dall'equipaggiamento e si
    /// rileggono quando cambia; la vita bassa si guarda al momento del colpo.
    /// </summary>
    public enum TalentCondition
    {
        /// <summary>Sempre.</summary>
        None,

        /// <summary>Con uno scudo in mano (Armatura del custode, Fortezza).</summary>
        Shield,

        /// <summary>Con un'arma a due mani (Presa a due mani).</summary>
        TwoHanded,

        /// <summary>Sotto il 30% della vita (Ultima difesa).</summary>
        LowLife,
    }
}
