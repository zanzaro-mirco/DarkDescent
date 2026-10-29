using System;

namespace DarkDescent.Items
{
    /// <summary>
    /// Che cosa tratta un banco del borgo (D18 della M18): il fabbro armi e armature, la guaritrice
    /// le pozioni, il mercante anelli, amuleti e pergamene. Vende e compra solo questi tipi.
    /// </summary>
    [Flags]
    public enum ShopGoods
    {
        None = 0,
        Weapons = 1,

        /// <summary>Armature, elmi, guanti, stivali e scudi.</summary>
        Armor = 2,

        /// <summary>Anelli e amuleti.</summary>
        Jewelry = 4,
        Potions = 8,
        Scrolls = 16,
        All = Weapons | Armor | Jewelry | Potions | Scrolls,
    }
}
