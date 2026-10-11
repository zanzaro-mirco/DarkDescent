using System;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il prezzo di un oggetto (D4 della M10): valore base del tipo × rarità × (1 + livello / 10),
    /// almeno 1. Il mercante vende a questo prezzo e compra al 25%. Calcolato, così non serve un
    /// prezzo per ogni combinazione di affissi.
    /// </summary>
    public static class ItemPricing
    {
        /// <summary>Quanto paga il mercante, in parti del prezzo pieno.</summary>
        public const int SellDivisor = 4;

        public static int RarityMultiplier(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Magic: return 3;
                case Rarity.Rare: return 6;
                default: return 1;
            }
        }

        /// <summary>Il prezzo pieno, quello a cui il mercante vende.</summary>
        public static int Price(ItemInstance item)
        {
            if (item?.Definition == null)
            {
                return 0;
            }

            double price = item.Definition.Value * RarityMultiplier(item.Rarity) * (1.0 + item.ItemLevel / 10.0);
            return Math.Max(1, (int)Math.Round(price, MidpointRounding.AwayFromZero));
        }

        /// <summary>Quanto paga il mercante per l'oggetto: un quarto del prezzo, almeno 1.</summary>
        public static int SellPrice(ItemInstance item)
        {
            return item?.Definition == null ? 0 : Math.Max(1, Price(item) / SellDivisor);
        }
    }
}
