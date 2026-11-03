namespace DarkDescent.Talents
{
    /// <summary>
    /// Il numero di un'abilità che un nodo cambia (M20). Il valore del nodo si somma a quello
    /// dell'abilità, nella sua unità: secondi, mana, metri, punti percentuali.
    /// </summary>
    public enum AbilityParam
    {
        /// <summary>Secondi di ricarica; negativo per accorciarla.</summary>
        Cooldown,

        /// <summary>Mana; negativo per farla costare meno.</summary>
        ManaCost,

        /// <summary>Punti percentuali di danno in più.</summary>
        Damage,

        /// <summary>Secondi di durata in più.</summary>
        Duration,

        /// <summary>Metri di raggio in più; da zero, un'area che l'abilità non aveva (Carica furiosa).</summary>
        Radius,

        /// <summary>Secondi di stordimento in più.</summary>
        Stun,

        /// <summary>Punti percentuali della vita massima curati.</summary>
        Heal,

        /// <summary>Punti percentuali in più alla soglia della vita del nemico (Esecuzione).</summary>
        Threshold,

        /// <summary>Secondi di rallentamento in più ai nemici colpiti (Freddo penetrante, M21).</summary>
        Slow,

        /// <summary>Secondi di congelamento in più (Gelo profondo, M21).</summary>
        Freeze,
    }
}
