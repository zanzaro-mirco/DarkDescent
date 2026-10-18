namespace DarkDescent.Enemies
{
    /// <summary>Cosa fa un modificatore dei campioni (D5 della M12): il comportamento sta nel codice, i numeri nell'asset.</summary>
    public enum ChampionModifierKind
    {
        /// <summary>Si muove e colpisce più in fretta.</summary>
        Fast,

        /// <summary>Il colpo rallenta il cavaliere per qualche secondo.</summary>
        Frozen,

        /// <summary>Rigenera la vita, ma non subito dopo essere stato colpito.</summary>
        Regenerating,

        /// <summary>Non barcolla quando è colpito, e lo scudo non ne blocca i colpi.</summary>
        Unstoppable,

        /// <summary>Sotto una parte della vita fa più danno.</summary>
        Enraged,
    }
}
