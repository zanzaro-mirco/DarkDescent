using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DarkDescent.UI
{
    /// <summary>
    /// Legge la tabella di <c>CREDITS.md</c> per i riconoscimenti che scorrono dopo la vittoria (D9
    /// della M10): di ogni asset di terzi il nome dell'opera (la prima colonna fino alla parentesi o ai
    /// due punti), l'autore e la licenza, senza link, corsivi e codice. Le righe di quello che è
    /// stato fatto per il progetto si saltano: i riconoscimenti le dicono in una riga sola. Logica pura.
    /// </summary>
    public static class CreditsTable
    {
        /// <summary>Come comincia l'autore delle righe fatte per il progetto.</summary>
        public const string OwnWorkPrefix = "creat";

        private static readonly Regex Link = new Regex(@"\[([^\]]*)\]\([^)]*\)");
        private static readonly Regex Marks = new Regex(@"[*`]");

        // il sito in fondo all'autore: ", kaylousberg.com", ", github.com/NDISCOVER/Cinzel"; non ", Inc."
        private static readonly Regex Site = new Regex(@",\s*[\w-]+(\.[\w-]+)+(/\S*)?$");

        public static List<CreditEntry> Parse(string markdown)
        {
            var entries = new List<CreditEntry>();
            if (string.IsNullOrEmpty(markdown))
            {
                return entries;
            }

            foreach (string raw in markdown.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("|") || line.StartsWith("|---") || line.StartsWith("| Asset"))
                {
                    continue;
                }

                string[] cells = line.Trim('|').Split('|');
                if (cells.Length < 3)
                {
                    continue;
                }

                string author = Clean(cells[1]);
                if (author.StartsWith(OwnWorkPrefix))
                {
                    continue;
                }

                entries.Add(new CreditEntry(Head(cells[0]), Site.Replace(author, string.Empty), Head(cells[2])));
            }

            return entries;
        }

        // la parte che conta: prima della parentesi (i file, una nota) o del punto e virgola
        private static string Head(string cell)
        {
            string text = Clean(cell);
            int cut = text.IndexOf(" (");
            if (cut > 0)
            {
                text = text.Substring(0, cut);
            }

            cut = text.IndexOf(';');
            return (cut > 0 ? text.Substring(0, cut) : text).Trim();
        }

        private static string Clean(string cell)
        {
            string text = Link.Replace(cell, "$1");
            text = Marks.Replace(text, string.Empty);
            return text.Trim();
        }
    }
}
