namespace DarkDescent.Items
{
    /// <summary>
    /// Le rarità di un oggetto (D2 della M5): quanti affissi ha e di che colore è il nome. Unici e
    /// pezzi di set (D2 della M16) nascono solo da una definizione scritta a mano, mai dalla tabella
    /// delle rarità. Si salvano come numero: le nuove vanno in fondo.
    /// </summary>
    public enum Rarity
    {
        Normal,
        Magic,
        Rare,
        Unique,
        Set,
    }
}
