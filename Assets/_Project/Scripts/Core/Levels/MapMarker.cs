namespace DarkDescent.Levels
{
    /// <summary>
    /// Un carattere della mappa diverso da pavimento e roccia: uno scheletro, una torcia, una scala.
    /// La cella sotto un marcatore è pavimento.
    /// </summary>
    public readonly struct MapMarker
    {
        public MapMarker(char symbol, int x, int y)
        {
            Symbol = symbol;
            X = x;
            Y = y;
        }

        public char Symbol { get; }

        public int X { get; }

        public int Y { get; }
    }
}
