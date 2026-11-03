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

        /// <summary>
        /// Il colpo dell'arma di sempre, nella casella del sinistro (D18 della M20): non si lancia, lo
        /// usa il click su un nemico. Senza mana e senza ricarica.
        /// </summary>
        Attack,

        // le abilità del Mago (D3 della M21): il Dardo di fuoco, l'Anello, la Palla di fuoco, il Dardo di ghiaccio, la Nova e l'Armatura di ghiaccio
        // sono proiettili, aree e potenziamenti di sempre con i numeri nuovi dell'asset; questi sono i tipi che mancavano

        /// <summary>
        /// Il colpo del sinistro del Mago: un dardo dalla bacchetta o dal bastone, a distanza, senza mana e senza
        /// ricarica. Lo usa il click su un nemico, come l'Attacco, ma parte da lontano.
        /// </summary>
        ArcaneBolt,

        /// <summary>Si sposta subito sul punto indicato, passando sopra i nemici: il Passo di luce.</summary>
        Teleport,

        /// <summary>Per un po' il danno preso scala il mana invece della vita: lo Scudo di mana.</summary>
        ManaShield,

        /// <summary>Dopo un attimo una meteora cade sul punto indicato: la Meteora.</summary>
        Meteor,

        /// <summary>Un'area che resta e colpisce a ogni battito chi ci sta dentro: il Muro di fiamme (una riga) e la Tempesta di neve (un cerchio).</summary>
        Zone,
    }
}
