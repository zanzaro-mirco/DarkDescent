using System;

namespace DarkDescent.Items
{
    /// <summary>Su quali oggetti può comparire un affisso. Con le armi nuove delle classi future si aggiungono valori.</summary>
    [Flags]
    public enum AffixTargets
    {
        None = 0,
        Weapon = 1 << 0,
        Shield = 1 << 1,
        All = Weapon | Shield,
    }
}
