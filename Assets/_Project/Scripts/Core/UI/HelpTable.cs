using System.Collections.Generic;

namespace DarkDescent.UI
{
    /// <summary>Che tasto va scritto accanto a una riga del pannello dei comandi (D14 della M23).</summary>
    public enum HelpKey
    {
        LeftClick,
        RightClick,
        Wheel,
        Move,
        Rotate,
        Skills,
        Belt,
        Portal,
        Inventory,
        Character,
        SkillMenu,
        SkillTree,
        Map,
        Pause,
        Labels,
        Help,
    }

    /// <summary>Una riga: il tasto e la sua descrizione; per il Mago la descrizione può essere un'altra (il sinistro lancia il dardo).</summary>
    public sealed class HelpRow
    {
        public HelpRow(HelpKey key, string textKey, string mageTextKey = null)
        {
            Key = key;
            TextKey = textKey;
            MageTextKey = mageTextKey;
        }

        public HelpKey Key { get; }

        public string TextKey { get; }

        /// <summary>La descrizione per il Mago, o null se è la stessa di tutti.</summary>
        public string MageTextKey { get; }

        /// <summary>La descrizione per quella classe: il Mago ha la sua dove serve.</summary>
        public string TextFor(bool mage) => mage && MageTextKey != null ? MageTextKey : TextKey;
    }

    public sealed class HelpGroup
    {
        public HelpGroup(string titleKey, params HelpRow[] rows)
        {
            TitleKey = titleKey;
            Rows = rows;
        }

        public string TitleKey { get; }

        public IReadOnlyList<HelpRow> Rows { get; }
    }

    /// <summary>
    /// Il pannello dei comandi (D14 della M23), come dati: quattro gruppi in due colonne, <b>Movimento</b> e
    /// <b>Vedere</b> a sinistra, <b>Combattimento</b> e <b>Finestre</b> a destra. I testi stanno nella tabella delle
    /// stringhe, nelle due lingue; i tasti si leggono dai binding quando il pannello si apre.
    /// </summary>
    public static class HelpTable
    {
        public const string TitleKey = "help.title";
        public const string CloseKey = "help.close";
        public const string LeftClickKey = "help.key.left_click";
        public const string RightClickKey = "help.key.right_click";
        public const string WheelKey = "help.key.wheel";

        private static readonly HelpGroup[] LeftColumn =
        {
            new HelpGroup(
                "help.group.move",
                new HelpRow(HelpKey.LeftClick, "help.row.click"),
                new HelpRow(HelpKey.Move, "help.row.keys"),
                new HelpRow(HelpKey.Rotate, "help.row.rotate"),
                new HelpRow(HelpKey.Wheel, "help.row.zoom")),
            new HelpGroup(
                "help.group.see",
                new HelpRow(HelpKey.Labels, "help.row.labels"),
                new HelpRow(HelpKey.Help, "help.row.help")),
        };

        private static readonly HelpGroup[] RightColumn =
        {
            new HelpGroup(
                "help.group.combat",
                new HelpRow(HelpKey.LeftClick, "help.row.attack", "help.row.attack.mage"),
                new HelpRow(HelpKey.RightClick, "help.row.cast"),
                new HelpRow(HelpKey.Skills, "help.row.skills"),
                new HelpRow(HelpKey.Belt, "help.row.belt"),
                new HelpRow(HelpKey.Portal, "help.row.portal")),
            new HelpGroup(
                "help.group.windows",
                new HelpRow(HelpKey.Inventory, "help.row.inventory"),
                new HelpRow(HelpKey.Character, "help.row.character"),
                new HelpRow(HelpKey.SkillMenu, "help.row.skill_menu"),
                new HelpRow(HelpKey.SkillTree, "help.row.skill_tree"),
                new HelpRow(HelpKey.Map, "help.row.map"),
                new HelpRow(HelpKey.Pause, "help.row.pause")),
        };

        public static IReadOnlyList<HelpGroup> Left => LeftColumn;

        public static IReadOnlyList<HelpGroup> Right => RightColumn;

        /// <summary>Tutte le chiavi di testo che il pannello legge (per il test di copertura delle lingue).</summary>
        public static IEnumerable<string> TextKeys()
        {
            yield return TitleKey;
            yield return CloseKey;
            yield return LeftClickKey;
            yield return RightClickKey;
            yield return WheelKey;
            foreach (var column in new[] { LeftColumn, RightColumn })
            {
                foreach (var group in column)
                {
                    yield return group.TitleKey;
                    foreach (var row in group.Rows)
                    {
                        yield return row.TextKey;
                        if (row.MageTextKey != null)
                        {
                            yield return row.MageTextKey;
                        }
                    }
                }
            }
        }
    }
}
