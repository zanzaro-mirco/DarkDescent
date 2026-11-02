namespace DarkDescent.Talents
{
    /// <summary>Che cosa cambia un effetto di un nodo (M20).</summary>
    public enum TalentEffectKind
    {
        /// <summary>Un modificatore della scheda, come gli affissi: +3% danno, +4% vita.</summary>
        Stat,

        /// <summary>Un numero di un'abilità: la ricarica della Carica, il raggio del Turbine.</summary>
        Ability,

        /// <summary>Qualcosa che succede a un colpo, a un'uccisione o a un blocco: mana, danno rimandato.</summary>
        Trigger,
    }
}
