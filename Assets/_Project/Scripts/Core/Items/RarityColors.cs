using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// I colori delle rarità, come in Diablo: bianco, blu, giallo, e dalla M16 oro scuro per gli
    /// unici e verde per i set (D2). Il testo per nomi ed etichette, la luce per gli oggetti a terra,
    /// più satura perché si veda nel buio.
    /// </summary>
    public static class RarityColors
    {
        public static string TextHex(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return "#7F9CFF";
                case Rarity.Rare: return "#FFD84A";
                case Rarity.Unique: return "#C9A55C";
                case Rarity.Set: return "#4FD45A";
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
                case Rarity.Unique: return new Color(0.4f, 0.26f, 0.08f, 0.4f);
                case Rarity.Set: return new Color(0.08f, 0.38f, 0.1f, 0.35f);
                default: return new Color(1f, 1f, 1f, 0.05f);
            }
        }

        public static Color Light(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return new Color(0.45f, 0.6f, 1f);
                case Rarity.Rare: return new Color(1f, 0.85f, 0.3f);
                case Rarity.Unique: return new Color(1f, 0.62f, 0.2f);
                case Rarity.Set: return new Color(0.3f, 1f, 0.35f);
                default: return new Color(1f, 0.82f, 0.55f);
            }
        }

        /// <summary>
        /// Quanto è più forte la luce a terra (D2 della M16): unici e set si vedono da lontano, più dei
        /// rari. Uno per gli altri.
        /// </summary>
        public static float LightBoost(Rarity rarity)
        {
            return rarity >= Rarity.Unique ? 1.6f : 1f;
        }
    }
}
