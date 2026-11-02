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

        /// <summary>Mana massimo in più, oltre a quello di Magia e livello (gli affissi della M9).</summary>
        Mana,

        /// <summary>Punti percentuali in più al danno dei colpi e degli incantesimi (il santuario della Furia, M13).</summary>
        DamageBonus,

        /// <summary>Punti percentuali in più all'esperienza dei nemici uccisi (il santuario della Sapienza, M13).</summary>
        ExperienceBonus,

        /// <summary>Punti percentuali in più alla velocità d'attacco (unici e set, D5 della M16).</summary>
        AttackSpeed,

        /// <summary>Percentuale del danno inflitto dai colpi che torna in vita (unici e set, D5 della M16).</summary>
        LifeSteal,

        /// <summary>Punti tolti al danno di ogni colpo preso (unici e set, D5 della M16).</summary>
        DamageReduction,

        /// <summary>Punti percentuali in più alla vita massima, Vitalità compresa (l'albero, M20).</summary>
        MaxLifePercent,

        /// <summary>Punti in più alla probabilità di blocco con lo scudo; il tetto resta 75 (l'albero, M20).</summary>
        BlockChance,

        /// <summary>Punti percentuali tolti al danno di ogni colpo preso (l'albero, M20).</summary>
        DamageResist,

        /// <summary>Punti percentuali in più al danno dei critici: 30 porta il ×2 a ×2,3 (l'albero, M20).</summary>
        CritDamage,

        /// <summary>Punti percentuali in più alla velocità di movimento (l'albero, M20).</summary>
        MoveSpeed,

        /// <summary>Punti percentuali in più alla cura delle pozioni (l'albero, M20).</summary>
        PotionBonus,

        /// <summary>
        /// Punti percentuali del danno fermati quando lo scudo avrebbe bloccato un colpo imparabile
        /// (la Fortezza dell'albero, M20).
        /// </summary>
        PiercedBlock,
    }
}
