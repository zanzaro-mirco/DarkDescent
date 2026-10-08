namespace DarkDescent.Items
{
    /// <summary>
    /// Dove si equipaggia un oggetto. Arma e scudo dalla M4 (D8); dalla M8 elmo, armatura, guanti,
    /// stivali, amuleto e due anelli (D4). Un anello dice di andare in <see cref="Ring"/>, ma entra
    /// anche in <see cref="Ring2"/>: è l'equipaggiamento a scegliere quale. I valori numerici
    /// finiscono nei salvataggi: le voci nuove vanno in fondo.
    /// </summary>
    public enum EquipSlot
    {
        None,
        Weapon,
        Offhand,
        Helm,
        Body,
        Gloves,
        Boots,
        Amulet,
        Ring,

        /// <summary>Il secondo anello: nessun oggetto lo chiede, ci va un anello quando il primo è occupato.</summary>
        Ring2,
    }
}
