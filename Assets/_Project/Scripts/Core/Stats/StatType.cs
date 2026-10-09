namespace DarkDescent.Stats
{
    /// <summary>
    /// Le statistiche di un personaggio: i quattro attributi del piano (§ 2), l'Armatura e, dalla
    /// M5, i bonus degli affissi. I valori numerici fanno da indice: nuove voci vanno in fondo.
    /// </summary>
    public enum StatType
    {
        Strength,
        Dexterity,
        Magic,
        Vitality,
        Armor,

        /// <summary>Punti percentuali in più alla probabilità di colpire.</summary>
        ToHit,

        /// <summary>Vita massima in più, oltre a quella data dalla Vitalità.</summary>
        Life,

        /// <summary>Punti percentuali in più alla probabilità di critico (gli affissi dei gioielli, M8).</summary>
        CritChance,

        /// <summary>
        /// Il livello del personaggio, scritto dalla sua crescita (M8): entra nel danno con la Forza.
        /// I nemici non ce l'hanno, e vale 0.
        /// </summary>
        Level,
    }
}
