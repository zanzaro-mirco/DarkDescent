using System;
using DarkDescent.Items;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Un oggetto rimasto a terra in un livello, come lo si ricorda uscendo (10.9 della M10): l'istanza
    /// con i suoi affissi, o l'oro di una pila, e il punto dove sta.
    /// </summary>
    [Serializable]
    public sealed class GroundItemRecord
    {
        [SerializeField] private ItemInstance _item;
        [SerializeField] private int _gold;
        [SerializeField] private Vector3 _position;

        /// <summary>Un oggetto a terra.</summary>
        public GroundItemRecord(ItemInstance item, Vector3 position)
        {
            _item = item;
            _position = position;
        }

        /// <summary>Una pila d'oro a terra.</summary>
        public GroundItemRecord(int gold, Vector3 position)
        {
            _gold = gold;
            _position = position;
        }

        /// <summary>L'oggetto; null per una pila d'oro.</summary>
        public ItemInstance Item => _gold > 0 ? null : _item;

        /// <summary>L'oro della pila; 0 per un oggetto.</summary>
        public int Gold => _gold;

        public Vector3 Position => _position;
    }
}
