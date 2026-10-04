namespace DarkDescent.Items
{
    /// <summary>
    /// Cosa fa un affisso (D3 della M5). Quelli "dell'oggetto" cambiano i numeri dell'arma o dello
    /// scudo; gli altri passano dallo StatSheet del personaggio con l'oggetto come sorgente.
    /// </summary>
    public enum AffixEffect
    {
        /// <summary>Dell'oggetto: + percentuale del danno dell'arma.</summary>
        WeaponDamagePercent,

        /// <summary>Dell'oggetto: + percentuale dell'Armatura dello scudo.</summary>
        ArmorPercent,

        /// <summary>Dell'oggetto: + punti di blocco dello scudo.</summary>
        BlockChance,

        /// <summary>Del personaggio: + Armatura.</summary>
        Armor,

        /// <summary>Del personaggio: + a colpire, in punti percentuali.</summary>
        ToHit,

        Strength,
        Dexterity,
        Vitality,

        /// <summary>Del personaggio: + vita massima.</summary>
        Life,
    }
}
