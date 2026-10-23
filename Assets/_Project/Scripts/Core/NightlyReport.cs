using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DarkDescent.Core
{
    /// <summary>
    /// Il rapporto della ronda notturna (D8 della M15): un file Markdown a cui ogni parte aggiunge la
    /// sua sezione, con i numeri, gli errori e il seme che li rifà. Il workflow lo mette nella pagina
    /// del run e nell'artefatto. Senza un percorso non si scrive niente: i test girano uguali.
    /// </summary>
    public static class NightlyReport
    {
        /// <summary>Il percorso del rapporto sulla riga di comando di Unity, come lo passa il workflow (relativo alla cartella del progetto).</summary>
        public const string PathOption = "-nightly-report";

        /// <summary>Il percorso del rapporto per una prova in locale.</summary>
        public const string PathVariable = "DARKDESCENT_NIGHTLY_REPORT";

        /// <summary>Gli errori scritti per intero in una parte: gli altri sono solo contati.</summary>
        public const int ShownErrors = 10;

        /// <summary>Il percorso di questa esecuzione: riga di comando, poi variabile; null se non c'è.</summary>
        public static string FilePath()
        {
            return FilePath(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable(PathVariable));
        }

        public static string FilePath(string[] args, string variable)
        {
            if (CommandLine.TryGetValue(args, PathOption, out string path) && !string.IsNullOrWhiteSpace(path))
            {
                return path.Trim();
            }

            return string.IsNullOrWhiteSpace(variable) ? null : variable.Trim();
        }

        /// <summary>
        /// Una parte della ronda: il titolo, l'esito con i numeri, i dettagli (una tabella, le misure) e
        /// i primi errori in un blocco che si apre, con il testo com'è (le mappe restano mappe).
        /// </summary>
        public static string Part(string title, string summary, IReadOnlyList<string> errors, string details = null)
        {
            int count = errors?.Count ?? 0;
            var text = new StringBuilder();
            text.Append("### ").Append(title).Append("\n\n");
            text.Append(count == 0 ? "✅ " : "❌ ").Append(summary).Append("\n\n");
            if (!string.IsNullOrEmpty(details))
            {
                text.Append(details.TrimEnd('\n')).Append("\n\n");
            }

            if (count > 0)
            {
                int shown = Math.Min(count, ShownErrors);
                text.Append("<details><summary>")
                    .Append(count == 1 ? "1 errore" : count == shown ? $"{count} errori" : $"{count} errori, i primi {shown}")
                    .Append("</summary>\n\n```\n")
                    .Append(string.Join("\n\n", errors.Take(shown)).Replace("```", "'''"))
                    .Append("\n```\n\n</details>\n\n");
            }

            return text.ToString();
        }

        /// <summary>Aggiunge al rapporto di questa esecuzione, se ce n'è uno.</summary>
        public static void Append(string text)
        {
            string path = FilePath();
            if (path != null)
            {
                Append(path, text);
            }
        }

        public static void Append(string path, string text)
        {
            string folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.AppendAllText(path, text, new UTF8Encoding(false));
        }
    }
}
