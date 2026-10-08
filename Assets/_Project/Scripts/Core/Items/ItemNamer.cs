using System.Text;
using DarkDescent.Localization;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il nome di un oggetto nella lingua attiva (D4 della M5): lo schema della lingua
    /// (<c>item.name.pattern</c>, in inglese "{prefix} {base} {suffix}", in italiano
    /// "{base} {prefix} {suffix}") con il primo prefisso accordato al genere della base e il primo
    /// suffisso. Il genere è n, m o f, e dalla M8 mp per le basi al plurale maschile (guanti,
    /// stivali). Il nome non si salva mai: si compone quando serve.
    /// </summary>
    public static class ItemNamer
    {
        public const string PatternKey = "item.name.pattern";
        public const string GenderSuffix = ".gender";
        public const string FeminineSuffix = ".f";
        public const string MasculinePluralSuffix = ".mp";

        private static readonly StringBuilder Builder = new StringBuilder(64);

        public static string Name(ItemInstance item, Localizer localizer)
        {
            string baseName = localizer.Get(item.Definition.NameKey);
            AffixDefinition prefix = First(item, AffixKind.Prefix);
            AffixDefinition suffix = First(item, AffixKind.Suffix);
            if (prefix == null && suffix == null)
            {
                return baseName;
            }

            string prefixName = string.Empty;
            if (prefix != null)
            {
                string gender = localizer.Get(item.Definition.NameKey + GenderSuffix);
                string form = gender == "f" ? FeminineSuffix : gender == "mp" ? MasculinePluralSuffix : string.Empty;
                prefixName = localizer.Get(prefix.NameKey + form);
            }

            string suffixName = suffix != null ? localizer.Get(suffix.NameKey) : string.Empty;

            // lo schema, poi via gli spazi doppi lasciati da un pezzo che manca
            Builder.Clear();
            Builder.Append(localizer.Get(PatternKey));
            Builder.Replace("{prefix}", prefixName).Replace("{base}", baseName).Replace("{suffix}", suffixName);
            for (int i = Builder.Length - 1; i > 0; i--)
            {
                if (Builder[i] == ' ' && Builder[i - 1] == ' ')
                {
                    Builder.Remove(i, 1);
                }
            }

            return Builder.ToString().Trim();
        }

        private static AffixDefinition First(ItemInstance item, AffixKind kind)
        {
            foreach (var affix in item.Affixes)
            {
                if (affix.Definition != null && affix.Definition.Kind == kind)
                {
                    return affix.Definition;
                }
            }

            return null;
        }
    }
}
