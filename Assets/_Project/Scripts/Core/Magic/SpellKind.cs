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
    }
}
