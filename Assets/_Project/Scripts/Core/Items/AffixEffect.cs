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

        /// <summary>Del personaggio: + probabilità di critico, in punti percentuali (gioielli, M8).</summary>
        CritChance,

        /// <summary>+Magia (M9): più mana e incantesimi più forti; su gioielli ed elmi.</summary>
        Magic,

        /// <summary>+mana massimo (M9); su gioielli ed elmi.</summary>
        Mana,

        /// <summary>Del personaggio: + percentuale di velocità d'attacco (unici e set, D5 della M16).</summary>
        AttackSpeed,

        /// <summary>Del personaggio: percentuale del danno inflitto che torna in vita (unici e set, D5 della M16).</summary>
        LifeSteal,

        /// <summary>Del personaggio: punti tolti a ogni colpo preso, mai sotto 1 (unici e set, D5 della M16).</summary>
        DamageReduction,

        /// <summary>Del personaggio: + Forza, Destrezza, Magia e Vitalità insieme (unici e set, D5 della M16).</summary>
        AllAttributes,

        /// <summary>Del personaggio: + percentuale del danno di colpi e incantesimi, come il santuario della Furia (M16).</summary>
        DamageBonus,

        /// <summary>Del personaggio: + percentuale dell'esperienza, come il santuario della Sapienza (M16).</summary>
        ExperienceBonus,
    }
}
