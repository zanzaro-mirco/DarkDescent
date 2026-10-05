using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// La griglia dell'inventario, senza dipendenze dalla scena: ogni oggetto occupa un rettangolo
    /// di celle grande quanto la sua definizione. La cella (0, 0) è in alto a sinistra.
    /// </summary>
    public sealed class InventoryGrid
    {
        private readonly ItemInstance[,] _cells;
        private readonly Dictionary<ItemInstance, RectInt> _placements = new Dictionary<ItemInstance, RectInt>();

        public InventoryGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "La griglia deve avere almeno una cella");
            }

            _cells = new ItemInstance[width, height];
        }

        /// <summary>Un oggetto è entrato, uscito o si è spostato.</summary>
        public event Action Changed;

        public int Width => _cells.GetLength(0);

        public int Height => _cells.GetLength(1);

        /// <summary>Gli oggetti nella griglia, con il rettangolo che occupano.</summary>
        public IReadOnlyDictionary<ItemInstance, RectInt> Placements => _placements;

        public ItemInstance ItemAt(Vector2Int cell)
        {
            return Contains(cell) ? _cells[cell.x, cell.y] : null;
        }

        public bool TryGetPlacement(ItemInstance item, out RectInt area)
        {
            return _placements.TryGetValue(item, out area);
        }

        /// <summary>Se l'oggetto entra con l'angolo in alto a sinistra in <paramref name="topLeft"/>, su celle libere.</summary>
        public bool CanPlace(ItemInstance item, Vector2Int topLeft)
        {
            return Fits(item, topLeft) && CountOccupants(AreaOf(item, topLeft), out _) == 0;
        }

        public bool TryPlace(ItemInstance item, Vector2Int topLeft)
        {
            if (item == null || _placements.ContainsKey(item) || !CanPlace(item, topLeft))
            {
                return false;
            }

            Occupy(item, AreaOf(item, topLeft));
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Posa l'oggetto; se sotto c'è un oggetto solo, lo scambia e lo restituisce in
        /// <paramref name="displaced"/>. Con due o più oggetti sotto, o fuori dai bordi, non fa niente.
        /// </summary>
        public bool TryPlaceOrSwap(ItemInstance item, Vector2Int topLeft, out ItemInstance displaced)
        {
            displaced = null;
            if (item == null || _placements.ContainsKey(item) || !Fits(item, topLeft))
            {
                return false;
            }

            RectInt area = AreaOf(item, topLeft);
            int occupants = CountOccupants(area, out ItemInstance occupant);
            if (occupants > 1)
            {
                return false;
            }

            if (occupant != null)
            {
                Free(occupant);
                displaced = occupant;
            }

            Occupy(item, area);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Il primo posto libero, colonna per colonna da sinistra e dall'alto, come in Diablo.</summary>
        public bool TryAutoPlace(ItemInstance item)
        {
            if (item?.Definition == null)
            {
                return false;
            }

            Vector2Int size = item.Definition.Size;
            for (int x = 0; x <= Width - size.x; x++)
            {
                for (int y = 0; y <= Height - size.y; y++)
                {
                    if (TryPlace(item, new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Remove(ItemInstance item)
        {
            if (item == null || !_placements.ContainsKey(item))
            {
                return false;
            }

            Free(item);
            Changed?.Invoke();
            return true;
        }

        private bool Contains(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        private bool Fits(ItemInstance item, Vector2Int topLeft)
        {
            if (item?.Definition == null)
            {
                return false;
            }

            Vector2Int size = item.Definition.Size;
            return topLeft.x >= 0 && topLeft.y >= 0 && topLeft.x + size.x <= Width && topLeft.y + size.y <= Height;
        }

        private static RectInt AreaOf(ItemInstance item, Vector2Int topLeft)
        {
            return new RectInt(topLeft, item.Definition.Size);
        }

        // quanti oggetti diversi stanno nell'area (0, 1, o 2 per "più di uno"); se è uno solo, quale
        private int CountOccupants(RectInt area, out ItemInstance single)
        {
            ItemInstance first = null;
            for (int x = area.xMin; x < area.xMax; x++)
            {
                for (int y = area.yMin; y < area.yMax; y++)
                {
                    ItemInstance occupant = _cells[x, y];
                    if (occupant == null)
                    {
                        continue;
                    }

                    if (first == null)
                    {
                        first = occupant;
                    }
                    else if (!ReferenceEquals(occupant, first))
                    {
                        single = null;
                        return 2;
                    }
                }
            }

            single = first;
            return first == null ? 0 : 1;
        }

        private void Occupy(ItemInstance item, RectInt area)
        {
            _placements[item] = area;
            SetCells(area, item);
        }

        private void Free(ItemInstance item)
        {
            SetCells(_placements[item], null);
            _placements.Remove(item);
        }

        private void SetCells(RectInt area, ItemInstance value)
        {
            for (int x = area.xMin; x < area.xMax; x++)
            {
                for (int y = area.yMin; y < area.yMax; y++)
                {
                    _cells[x, y] = value;
                }
            }
        }
    }
}
