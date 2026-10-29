namespace DarkDescent.Items
{
    /// <summary>A quale mestiere appartiene un oggetto (D18 della M18): dal tipo della sua definizione.</summary>
    public static class ItemGoods
    {
        /// <summary>Il tipo di merce di una definizione; None per l'oro e per quello che nessuno tratta.</summary>
        public static ShopGoods Of(ItemDefinition item)
        {
            switch (item)
            {
                case WeaponDefinition _:
                    return ShopGoods.Weapons;
                case ArmorDefinition _:
                    return ShopGoods.Armor;
                case JewelryDefinition _:
                    return ShopGoods.Jewelry;
                case PotionDefinition _:
                    return ShopGoods.Potions;
                case ScrollDefinition _:
                    return ShopGoods.Scrolls;
                default:
                    return ShopGoods.None;
            }
        }

        /// <summary>Il banco che tratta <paramref name="goods"/> vende e compra questo oggetto.</summary>
        public static bool Trades(this ShopGoods goods, ItemDefinition item)
        {
            var kind = Of(item);
            return kind != ShopGoods.None && (goods & kind) == kind;
        }
    }
}
