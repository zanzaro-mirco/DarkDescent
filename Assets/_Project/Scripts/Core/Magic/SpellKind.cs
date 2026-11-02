namespace DarkDescent.Magic
{
    /// <summary>Che cosa fa un incantesimo (D3 della M9): uno per tipo, come nel piano.</summary>
    public enum SpellKind
    {
        /// <summary>Un proiettile verso il cursore: il Dardo di fuoco.</summary>
        Projectile,

        /// <summary>Colpisce tutti i nemici attorno al cavaliere: l'Anello di fuoco.</summary>
        Area,

        /// <summary>Un effetto sul cavaliere per un po': la Pelle di pietra.</summary>
        Buff,

        /// <summary>
        /// Il portale per l'accampamento (passo 11.7 della M11), al posto della pergamena del ritorno:
        /// sta a sinistra della cintura, con il suo tasto, fuori dai posti F1–F3.
        /// </summary>
        Portal,

        // le abilità del Guerriero (D8 della M20): numeri negli asset, il comportamento nel passo 20.4

        /// <summary>Un colpo d'arma più forte: Colpo possente.</summary>
        Strike,

        /// <summary>La corsa sul nemico, con lo stordimento: Carica.</summary>
        Charge,

        /// <summary>Danno in più per un po': Grido di battaglia.</summary>
        Shout,

        /// <summary>Un colpo con lo scudo che stordisce: Colpo di scudo.</summary>
        ShieldBash,

        /// <summary>Tutti i colpi bloccabili fermati per un po': Muro di scudi.</summary>
        ShieldWall,

        /// <summary>Vita massima in più per un po', e curata: Ultimo baluardo.</summary>
        LastStand,

        /// <summary>Un colpo a tutti i nemici attorno: Turbine.</summary>
        Whirlwind,

        /// <summary>Un colpo più forte sui nemici feriti: Esecuzione.</summary>
        Execute,

        /// <summary>Il salto sul punto indicato, con lo stordimento: Salto.</summary>
        Leap,

        /// <summary>Il turbine che dura mentre si cammina: Tempesta d'acciaio.</summary>
        Bladestorm,
    }
}
