namespace DarkDescent.Items
{
    /// <summary>Che segreto nasconde una cassa normale (D3 e D4 della M25).</summary>
    public enum ChestSecretKind
    {
        None,

        /// <summary>Un nemico travestito da forziere: aprirlo lo fa saltare in piedi.</summary>
        Mimic,

        /// <summary>Il baule maledetto: aprirlo chiama un'ondata, e il premio è migliore.</summary>
        Cursed,
    }
}
