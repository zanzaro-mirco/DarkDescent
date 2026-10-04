using System.Text;
using DarkDescent.Items;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il testo del tooltip di un oggetto, in rich text di TextMesh Pro: nome, danno o Armatura,
    /// requisiti. Logica pura, così il formato si prova senza scena.
    /// </summary>
    public static class ItemDescription
    {
        /// <summary>Il rosso dei requisiti non soddisfatti, come in Diablo.</summary>
        public const string UnmetColor = "#D04848";

        /// <summary>
        /// Scrive la descrizione in <paramref name="builder"/>, dopo averlo svuotato.
        /// <paramref name="meetsRequirements"/> colora di rosso la Forza richiesta quando è false.
        /// </summary>
        public static void Write(StringBuilder builder, ItemDefinition definition, bool meetsRequirements)
        {
            builder.Clear();
            builder.Append("<b>").Append(definition.DisplayName).Append("</b>");

            switch (definition)
            {
                case WeaponDefinition weapon:
                    builder.Append("\nDanno: ").Append(weapon.MinDamage).Append('–').Append(weapon.MaxDamage);
                    if (weapon.RequiredStrength > 0)
                    {
                        builder.Append('\n');
                        if (!meetsRequirements)
                        {
                            builder.Append("<color=").Append(UnmetColor).Append('>');
                        }

                        builder.Append("Forza richiesta: ").Append(weapon.RequiredStrength);
                        if (!meetsRequirements)
                        {
                            builder.Append("</color>");
                        }
                    }

                    break;

                case ArmorDefinition armor:
                    builder.Append("\nArmatura: ").Append(armor.Armor);
                    break;
            }
        }
    }
}
