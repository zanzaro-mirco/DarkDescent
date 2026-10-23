using System;
using System.Globalization;

namespace DarkDescent.Core
{
    /// <summary>
    /// La ronda notturna della CI (M15): la categoria dei suoi test e il seme della notte (D4). Il seme
    /// viene dalla riga di comando, poi dalla variabile d'ambiente, poi dalla data UTC (aaaammgg):
    /// la stessa notte si rifà uguale, e un errore si rifà in locale con il seme del rapporto.
    /// </summary>
    public static class NightlyRun
    {
        /// <summary>La categoria dei test che girano solo di notte (D3): quelli di ogni push la escludono.</summary>
        public const string Category = "Nightly";

        /// <summary>Il seme della notte sulla riga di comando di Unity, come lo passa il workflow.</summary>
        public const string SeedOption = "-nightly-seed";

        /// <summary>Il seme della notte per una prova in locale.</summary>
        public const string SeedVariable = "DARKDESCENT_NIGHTLY_SEED";

        /// <summary>Il seme di questa esecuzione: riga di comando, variabile, o la data di oggi in UTC.</summary>
        public static ulong Seed()
        {
            return Seed(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable(SeedVariable), DateTime.UtcNow);
        }

        /// <summary>Il seme dagli ingressi dati; un valore che non è un numero si salta.</summary>
        public static ulong Seed(string[] args, string variable, DateTime utcNow)
        {
            if (CommandLine.TryGetValue(args, SeedOption, out string text) && TryParse(text, out ulong seed))
            {
                return seed;
            }

            return TryParse(variable, out seed) ? seed : FromDate(utcNow);
        }

        /// <summary>Il seme di una data: 24 ottobre 2026 è 20261024, come lo scrive il workflow.</summary>
        public static ulong FromDate(DateTime utcDate)
        {
            return (ulong)(utcDate.Year * 10000 + utcDate.Month * 100 + utcDate.Day);
        }

        private static bool TryParse(string text, out ulong seed)
        {
            seed = 0UL;
            return !string.IsNullOrWhiteSpace(text) && ulong.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out seed);
        }
    }
}
