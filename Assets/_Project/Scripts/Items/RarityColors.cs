using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// I colori delle rarità, come in Diablo: bianco, blu, giallo. Il testo per nomi ed etichette, la
    /// luce per gli oggetti a terra, più satura perché si veda nel buio.
    /// </summary>
    public static class RarityColors
    {
        public static string TextHex(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return "#7F9CFF";
                case Rarity.Rare: return "#FFD84A";
                default: return "#E8E2D4";
            }
        }

        /// <summary>
        /// Lo sfondo della cella nell'inventario: appena visibile per i normali, colorato per gli
        /// altri. Scuro apposta: in color space lineare la trasparenza della UI schiarisce molto.
        /// </summary>
        public static Color Cell(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return new Color(0.12f, 0.16f, 0.45f, 0.3f);
                case Rarity.Rare: return new Color(0.45f, 0.36f, 0.08f, 0.3f);
                default: return new Color(1f, 1f, 1f, 0.05f);
            }
        }

        public static Color Light(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return new Color(0.45f, 0.6f, 1f);
                case Rarity.Rare: return new Color(1f, 0.85f, 0.3f);
                default: return new Color(1f, 0.82f, 0.55f);
            }
        }
    }
}
