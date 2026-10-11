using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il banco del mercante (D5 della M10): le provviste, sempre lì, e gli oggetti di questo ritorno
    /// in città, che spariscono comprati. Compra al prezzo pieno di <see cref="ItemPricing"/>, vende
    /// al 25%. Logica pura: oro e borsa sono quelli dell'<see cref="Inventory"/>.
    /// </summary>
    public sealed class Shop
    {
        private readonly List<ItemInstance> _supplies = new List<ItemInstance>();
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();

        /// <summary>Le provviste o gli oggetti in vendita sono cambiati.</summary>
        public event Action Changed;

        /// <summary>Le provviste, come oggetti normali di livello 1: da mostrare e da prezzare.</summary>
        public IReadOnlyList<ItemInstance> Supplies => _supplies;

        public IReadOnlyList<ItemInstance> Stock => _stock;

        /// <summary>Quante cose ci sono sul banco: le provviste prima, poi gli oggetti.</summary>
        public int Count => _supplies.Count + _stock.Count;

        /// <summary>Le provviste della tabella e gli oggetti appena tirati, al posto di quelli di prima.</summary>
        public void Restock(IReadOnlyList<ItemDefinition> supplies, IReadOnlyList<ItemInstance> stock)
        {
            _supplies.Clear();
            if (supplies != null)
            {
                foreach (var supply in supplies)
                {
                    if (supply != null)
                    {
                        _supplies.Add(new ItemInstance(supply));
                    }
                }
            }

            _stock.Clear();
            if (stock != null)
            {
                _stock.AddRange(stock);
            }

            Changed?.Invoke();
        }

        /// <summary>La cosa al posto <paramref name="index"/> del banco, provviste prima; null fuori dal banco.</summary>
        public ItemInstance ItemAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                return null;
            }

            return index < _supplies.Count ? _supplies[index] : _stock[index - _supplies.Count];
        }

        /// <summary>
        /// Compra la cosa al posto <paramref name="index"/>: serve l'oro e un posto nella cintura o nella
        /// borsa. Una provvista resta sul banco, un oggetto ne sparisce. Se non riesce non tocca niente.
        /// </summary>
        public TradeResult Buy(Inventory inventory, int index)
        {
            var shown = ItemAt(index);
            if (shown == null)
            {
                return TradeResult.Nothing;
            }

            bool supply = index < _supplies.Count;
            var item = supply ? new ItemInstance(shown.Definition) : shown;
            int price = ItemPricing.Price(item);
            if (inventory.Gold < price)
            {
                return TradeResult.NotEnoughGold;
            }

            if (!inventory.CanPickUp(item))
            {
                return TradeResult.NoRoom;
            }

            inventory.TrySpendGold(price);
            inventory.TryPickUp(item);
            if (!supply)
            {
                _stock.RemoveAt(index - _supplies.Count);
                Changed?.Invoke();
            }

            return TradeResult.Done;
        }

        /// <summary>Vende l'oggetto della borsa in quella cella al 25% del prezzo; Nothing se la cella è vuota.</summary>
        public TradeResult SellAt(Inventory inventory, Vector2Int cell)
        {
            var item = inventory.Grid.ItemAt(cell);
            if (item == null)
            {
                return TradeResult.Nothing;
            }

            inventory.Grid.Remove(item);
            inventory.ReceiveGold(ItemPricing.SellPrice(item));
            return TradeResult.Done;
        }
    }
}
