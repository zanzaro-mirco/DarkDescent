using System.Collections.Generic;

namespace DarkDescent.UI
{
    /// <summary>
    /// I dieci suggerimenti del primo gioco (D11 della M23), nell'ordine in cui vengono mostrati quando ne
    /// scattano due insieme. Ognuno ha due righe nella tabella delle stringhe: <c>hint.&lt;id&gt;.title</c> e
    /// <c>hint.&lt;id&gt;.text</c>; il testo può avere segnaposto <c>{0}</c>, <c>{1}</c>, <c>{2}</c> per i tasti veri.
    /// </summary>
    public static class Hints
    {
        public const string Move = "move";
        public const string Enemy = "enemy";
        public const string Item = "item";
        public const string Potion = "potion";
        public const string Stairs = "stairs";
        public const string AttributePoint = "attribute";
        public const string TalentPoint = "talent";
        public const string Portal = "portal";
        public const string Skills = "skills";
        public const string Village = "village";

        private static readonly string[] Ordered =
        {
            Move, Enemy, Item, Potion, Stairs, AttributePoint, TalentPoint, Portal, Skills, Village,
        };

        /// <summary>Tutti gli id, nell'ordine di precedenza.</summary>
        public static IReadOnlyList<string> All => Ordered;

        public static string TitleKey(string id) => "hint." + id + ".title";

        public static string TextKey(string id) => "hint." + id + ".text";

        /// <summary>Il posto di un id nell'ordine; un id sconosciuto va in fondo.</summary>
        public static int OrderOf(string id)
        {
            int index = System.Array.IndexOf(Ordered, id);
            return index >= 0 ? index : Ordered.Length;
        }
    }
}
