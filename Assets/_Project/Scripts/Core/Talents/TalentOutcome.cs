namespace DarkDescent.Talents
{
    /// <summary>Che cosa succede quando scatta un effetto di un nodo (M20).</summary>
    public enum TalentOutcome
    {
        /// <summary>Torna mana (Disciplina, Contraccolpo, Massacro).</summary>
        Mana,

        /// <summary>Una parte del colpo bloccato torna a chi l'ha dato, in punti percentuali (Rappresaglia).</summary>
        Reflect,

        /// <summary>Una statistica in più per qualche secondo (Frenesia).</summary>
        Buff,

        /// <summary>Punti percentuali di danno in più al colpo successivo (Vendicatore).</summary>
        NextHit,

        /// <summary>Lancia gratis l'abilità del nodo, con la probabilità in punti percentuali (Scintilla, M21).</summary>
        Cast,
    }
}
