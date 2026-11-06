using System.Collections.Generic;
using System.Linq;

namespace DarkDescent.Levels
{
    /// <summary>Che arredo regge un testo: il leggio, la lapide con l'incisione, o un mucchio di carte (diario, lettera, biglietto).</summary>
    public enum LoreKind
    {
        Lectern,
        Tablet,
        Book,
    }

    /// <summary>
    /// La lore (D9 della M25): dodici testi del mondo, uno al più per livello, due livelli su tre. Quale testo sta a quale profondità lo decide il seme del
    /// mondo con una permutazione: nello stesso mondo un testo non si ripete mai (per costruzione, senza una lista di testi letti nel salvataggio: niente
    /// formato nuovo, D13), e un testo della cripta non compare nelle caverne. Funzioni pure del seme e della profondità.
    /// </summary>
    public static class LoreRules
    {
        public const int TextCount = 12;

        /// <summary>La cripta: profondità 1–4; le caverne: 5–8.</summary>
        public const int LastCryptDepth = 4;

        public const int LastDepth = 8;

        private const ulong GateKind = 10UL;
        private const ulong CryptOrderKind = 8UL;
        private const ulong CaveOrderKind = 9UL;
        private const ulong CellKind = 11UL;

        // i testi della cripta, quelli delle caverne e quelli di ovunque (12 testi della scheda)
        private static readonly int[] CryptOnly = { 1, 2, 3, 4, 5, 6 };
        private static readonly int[] CaveOnly = { 7, 8, 9, 10 };
        private static readonly int[] Anywhere = { 11, 12 };

        /// <summary>Il dominio del seme da cui si sceglie la cella dell'arredo.</summary>
        public static ulong SpotKind => CellKind;

        /// <summary>Il livello ha un testo: due livelli su tre, dalla profondità 1 alla 8.</summary>
        public static bool Has(ulong runSeed, int depth)
        {
            return depth >= 1 && depth <= LastDepth && Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, GateKind) % 3UL != 0UL;
        }

        /// <summary>Il numero del testo (1–12) di quel livello, 0 se non ne ha. Nello stesso mondo due profondità non danno mai lo stesso testo.</summary>
        public static int TextFor(ulong runSeed, int depth)
        {
            if (!Has(runSeed, depth))
            {
                return 0;
            }

            var crypt = Order(CryptOnly.Concat(Anywhere), runSeed, CryptOrderKind);
            if (depth <= LastCryptDepth)
            {
                return crypt[depth - 1];
            }

            // le caverne: i loro quattro, e i due di ovunque che la cripta non ha usato nelle sue quattro profondità (anche se un livello della cripta non ha il testo)
            var used = new HashSet<int>(crypt.Take(LastCryptDepth));
            var cave = Order(CaveOnly.Concat(Anywhere.Where(id => !used.Contains(id))), runSeed, CaveOrderKind);
            return cave[depth - LastCryptDepth - 1];
        }

        /// <summary>L'arredo del testo: il leggio per 1, 5 e 10, la lapide per 3, 6, 7 e 12, le carte per gli altri (diari, lettera, biglietto).</summary>
        public static LoreKind KindOf(int text)
        {
            switch (text)
            {
                case 1:
                case 5:
                case 10:
                    return LoreKind.Lectern;
                case 3:
                case 6:
                case 7:
                case 12:
                    return LoreKind.Tablet;
                default:
                    return LoreKind.Book;
            }
        }

        public static string TitleKey(int text)
        {
            return $"lore.{text}.title";
        }

        public static string BodyKey(int text)
        {
            return $"lore.{text}.text";
        }

        private static List<int> Order(IEnumerable<int> ids, ulong runSeed, ulong kind)
        {
            return ids.OrderBy(id => Core.SeedMixer.ForSecret(runSeed, 0, id, 0, kind)).ThenBy(id => id).ToList();
        }
    }
}
