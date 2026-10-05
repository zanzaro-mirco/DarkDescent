using System;
using System.Collections.Generic;
using System.Text;

namespace DarkDescent.Localization
{
    /// <summary>
    /// La tabella delle stringhe letta da un CSV: una riga per chiave, una colonna per lingua, con
    /// l'intestazione <c>key,en,it,…</c>. Campi tra virgolette per le virgole, virgolette raddoppiate
    /// dentro un campo, <c>\n</c> per andare a capo. Righe vuote e righe che iniziano con <c>#</c>
    /// sono ignorate. Una lingua nuova è una colonna in più (D12 della M5).
    /// </summary>
    public sealed class StringTable
    {
        private readonly string[] _languages;
        private readonly Dictionary<string, string[]> _rows = new Dictionary<string, string[]>(StringComparer.Ordinal);

        private StringTable(string[] languages)
        {
            _languages = languages;
        }

        /// <summary>I codici delle lingue, nell'ordine delle colonne.</summary>
        public IReadOnlyList<string> Languages => _languages;

        public IEnumerable<string> Keys => _rows.Keys;

        public int Count => _rows.Count;

        public static StringTable Parse(string csv)
        {
            if (csv == null)
            {
                throw new ArgumentNullException(nameof(csv));
            }

            StringTable table = null;
            var fields = new List<string>();
            var field = new StringBuilder();
            int lineNumber = 0;
            foreach (var rawLine in csv.Replace("\r", string.Empty).Split('\n'))
            {
                lineNumber++;
                if (rawLine.Length == 0 || rawLine[0] == '#')
                {
                    continue;
                }

                SplitLine(rawLine, fields, field, lineNumber);
                if (table == null)
                {
                    if (fields.Count < 2 || fields[0] != "key")
                    {
                        throw new FormatException("La prima riga deve essere key,<lingua>,…");
                    }

                    table = new StringTable(fields.GetRange(1, fields.Count - 1).ToArray());
                    continue;
                }

                string key = fields[0];
                if (key.Length == 0)
                {
                    throw new FormatException($"Riga {lineNumber}: chiave vuota");
                }

                if (fields.Count - 1 > table._languages.Length)
                {
                    throw new FormatException($"Riga {lineNumber} ({key}): più colonne delle lingue");
                }

                var values = new string[table._languages.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = i + 1 < fields.Count ? fields[i + 1].Replace("\\n", "\n") : string.Empty;
                }

                if (!table._rows.TryAdd(key, values))
                {
                    throw new FormatException($"Riga {lineNumber}: chiave ripetuta {key}");
                }
            }

            return table ?? throw new FormatException("Tabella vuota");
        }

        /// <summary>L'indice della colonna di una lingua, o -1 se la tabella non la conosce.</summary>
        public int IndexOf(string language)
        {
            return Array.IndexOf(_languages, language);
        }

        public bool Contains(string key)
        {
            return key != null && _rows.ContainsKey(key);
        }

        /// <summary>Il testo della chiave nella colonna; false se la chiave non c'è o la cella è vuota.</summary>
        public bool TryGet(string key, int languageIndex, out string value)
        {
            value = null;
            if (key == null || languageIndex < 0 || !_rows.TryGetValue(key, out var values))
            {
                return false;
            }

            value = values[languageIndex];
            return value.Length > 0;
        }

        private static void SplitLine(string line, List<string> fields, StringBuilder field, int lineNumber)
        {
            fields.Clear();
            field.Clear();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c != '"')
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else if (c == '"' && field.Length == 0)
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (quoted)
            {
                throw new FormatException($"Riga {lineNumber}: virgolette non chiuse");
            }

            fields.Add(field.ToString());
        }
    }
}
