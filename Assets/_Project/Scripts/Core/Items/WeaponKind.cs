namespace DarkDescent.Items
{
    /// <summary>
    /// Il tipo di un'arma (D7 della M5): un dato della definizione, così le armi delle classi future
    /// (a due mani, bacchette, bastoni, archi) aggiungono un valore e le loro regole, non una
    /// struttura nuova. I valori numerici sono salvati negli asset: i nuovi vanno in fondo.
    /// </summary>
    public enum WeaponKind
    {
        Unarmed,
        Dagger,
        Sword,
        Axe,
    }
}
