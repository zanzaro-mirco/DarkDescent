using System.Globalization;
using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il testo del tooltip di un oggetto, in rich text di TextMesh Pro, nella lingua attiva: nome
    /// composto nel colore della rarità, danno o Armatura e blocco già con gli affissi, requisiti,
    /// velocità d'attacco, poi una riga per affisso in blu, come in Diablo; per le pozioni la cura e come berle. Logica
    /// pura, così il formato si prova senza scena.
    /// </summary>
    public static class ItemDescription
    {
        /// <summary>Il rosso dei requisiti non soddisfatti, come in Diablo.</summary>
        public const string UnmetColor = "#D04848";

        /// <summary>Il colore delle righe degli affissi: quello dei magici.</summary>
        public const string AffixColor = "#7F9CFF";

        private const string HeaderColor = "#A09A8C";

        // il suggerimento sotto la pozione: più spento del testo, come un'istruzione e non un valore
        private const string HintColor = "#8C8577";

        /// <summary>
        /// La velocità d'attacco a parole, come in Diablo (seconda prova della build M7): il pugnale è
        /// veloce, la spada corta normale, la lama dello scheletro e l'ascia lente.
        /// </summary>
        public static string SpeedKey(float attackInterval)
        {
            return attackInterval < 0.9f ? TextKeys.SpeedFast : attackInterval > 1.05f ? TextKeys.SpeedSlow : TextKeys.SpeedNormal;
        }

        /// <summary>
        /// Scrive la descrizione in <paramref name="builder"/>, dopo averlo svuotato.
        /// <paramref name="meetsRequirements"/> colora di rosso la Forza richiesta quando è false;
        /// <paramref name="header"/>, se c'è, va in piccolo sopra il nome ("Equipped").
        /// </summary>
        public static void Write(StringBuilder builder, ItemInstance item, bool meetsRequirements, Localizer localizer, string header = null)
        {
            builder.Clear();
            if (!string.IsNullOrEmpty(header))
            {
                builder.Append("<size=80%><color=").Append(HeaderColor).Append('>').Append(header).Append("</color></size>\n");
            }

            builder.Append("<b><color=").Append(RarityColors.TextHex(item.Rarity)).Append('>')
                .Append(ItemNamer.Name(item, localizer)).Append("</color></b>");

            switch (item.Definition)
            {
                case WeaponDefinition weapon:
                    var (min, max) = ItemStats.WeaponDamage(item);
                    builder.Append('\n').AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipDamage), min, max);
                    builder.Append('\n').AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipSpeed), localizer.Get(SpeedKey(weapon.AttackInterval)));
                    AppendRequirement(builder, weapon, meetsRequirements, localizer);
                    break;

                case ArmorDefinition armor:
                    builder.Append('\n').AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipArmor), ItemStats.ItemArmor(item));
                    if (armor.IsShield)
                    {
                        builder.Append('\n').AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipBlock), ItemStats.ShieldBlock(item));
                    }

                    AppendRequirement(builder, armor, meetsRequirements, localizer);
                    break;

                case PotionDefinition potion:
                    builder.Append('\n').AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipHeal), (int)System.Math.Round(potion.HealFraction * 100f));
                    builder.Append("\n<size=85%><color=").Append(HintColor).Append('>').Append(localizer.Get(TextKeys.TooltipDrink)).Append("</color></size>");
                    break;
            }

            foreach (var affix in item.Affixes)
            {
                if (affix.Definition == null)
                {
                    continue;
                }

                builder.Append("\n<color=").Append(AffixColor).Append('>')
                    .AppendFormat(CultureInfo.InvariantCulture, localizer.Get(EffectKey(affix.Definition.Effect)), affix.Value)
                    .Append("</color>");
            }
        }

        // la Forza richiesta, in rosso se non basta; niente riga per chi non ne chiede
        private static void AppendRequirement(StringBuilder builder, ItemDefinition definition, bool meetsRequirements, Localizer localizer)
        {
            if (definition.RequiredStrength <= 0)
            {
                return;
            }

            builder.Append('\n');
            if (!meetsRequirements)
            {
                builder.Append("<color=").Append(UnmetColor).Append('>');
            }

            builder.AppendFormat(CultureInfo.InvariantCulture, localizer.Get(TextKeys.TooltipRequiredStrength), definition.RequiredStrength);
            if (!meetsRequirements)
            {
                builder.Append("</color>");
            }
        }

        /// <summary>La chiave della riga di un effetto, come "+{0}% damage".</summary>
        public static string EffectKey(AffixEffect effect)
        {
            switch (effect)
            {
                case AffixEffect.WeaponDamagePercent: return TextKeys.EffectWeaponDamagePercent;
                case AffixEffect.ArmorPercent: return TextKeys.EffectArmorPercent;
                case AffixEffect.BlockChance: return TextKeys.EffectBlockChance;
                case AffixEffect.Armor: return TextKeys.EffectArmor;
                case AffixEffect.ToHit: return TextKeys.EffectToHit;
                case AffixEffect.Strength: return TextKeys.EffectStrength;
                case AffixEffect.Dexterity: return TextKeys.EffectDexterity;
                case AffixEffect.Vitality: return TextKeys.EffectVitality;
                case AffixEffect.CritChance: return TextKeys.EffectCritChance;
                default: return TextKeys.EffectLife;
            }
        }
    }
}
