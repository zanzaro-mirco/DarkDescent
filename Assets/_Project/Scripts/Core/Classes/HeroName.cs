using System.Text;

namespace DarkDescent.Classes
{
    /// <summary>
    /// Il nome del personaggio scelto alla creazione (D8 della M21): fino a 16 lettere, senza spazi ai
    /// bordi né a doppio, e senza segni che il testo del gioco leggerebbe come formattazione (il nome finisce
    /// nei posti del menu, che sono testo ricco). Senza niente di valido torna il nome di partenza della classe.
    /// </summary>
    public static class HeroName
    {
        public const int MaxLength = 16;

        public static string Clean(string raw, string fallback)
        {
            var text = new StringBuilder();
            bool space = false;
            foreach (char c in raw ?? "")
            {
                if (char.IsWhiteSpace(c))
                {
                    // gli spazi di mezzo (anche tab e a capo) si riducono a uno, quelli all'inizio spariscono
                    space = text.Length > 0;
                    continue;
                }

                if (c == '<' || c == '>' || char.IsControl(c))
                {
                    continue;
                }

                if (space)
                {
                    text.Append(' ');
                    space = false;
                }

                text.Append(c);
            }

            string name = text.Length > MaxLength ? text.ToString(0, MaxLength).TrimEnd() : text.ToString();
            return name.Length > 0 ? name : fallback ?? "";
        }
    }
}
