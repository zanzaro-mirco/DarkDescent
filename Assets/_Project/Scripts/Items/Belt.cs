using System;

namespace DarkDescent.Items
{
    /// <summary>
    /// La cintura (D14 della scheda M6): 8 posti per le pozioni, uno per tasto da 1 a 8. Accetta solo
    /// pozioni; un posto vuoto resta vuoto finché non ci si mette qualcosa, come in Diablo 1.
    /// Logica pura: la UI e i tasti la chiamano e basta.
    /// </summary>
    public sealed class Belt
    {
        public const int Size = 8;

        private readonly ItemInstance[] _slots = new ItemInstance[Size];

        /// <summary>Un posto ha cambiato oggetto; l'argomento è il posto, da 0.</summary>
        public event Action<int> Changed;

        /// <summary>Le pozioni nella cintura.</summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var item in _slots)
                {
                    count += item != null ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>Solo le pozioni vanno nella cintura.</summary>
        public static bool Accepts(ItemInstance item)
        {
            return item?.Definition is PotionDefinition;
        }

        /// <summary>L'oggetto nel posto, o null se è vuoto o fuori dalla cintura.</summary>
        public ItemInstance Get(int slot)
        {
            return slot >= 0 && slot < Size ? _slots[slot] : null;
        }

        /// <summary>Mette una pozione nel primo posto libero; false se non è una pozione o la cintura è piena.</summary>
        public bool TryAdd(ItemInstance item)
        {
            if (!Accepts(item))
            {
                return false;
            }

            for (int slot = 0; slot < Size; slot++)
            {
                if (_slots[slot] == null)
                {
                    Set(slot, item);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Toglie e restituisce l'oggetto del posto, o null se era vuoto.</summary>
        public ItemInstance Take(int slot)
        {
            ItemInstance item = Get(slot);
            if (item != null)
            {
                Set(slot, null);
            }

            return item;
        }

        /// <summary>
        /// Posa una pozione nel posto e restituisce in <paramref name="displaced"/> quella che c'era,
        /// o null. False, senza toccare niente, se non è una pozione o il posto non esiste.
        /// </summary>
        public bool TryPlaceOrSwap(int slot, ItemInstance item, out ItemInstance displaced)
        {
            displaced = null;
            if (!Accepts(item) || slot < 0 || slot >= Size)
            {
                return false;
            }

            displaced = _slots[slot];
            Set(slot, item);
            return true;
        }

        private void Set(int slot, ItemInstance item)
        {
            _slots[slot] = item;
            Changed?.Invoke(slot);
        }
    }
}
