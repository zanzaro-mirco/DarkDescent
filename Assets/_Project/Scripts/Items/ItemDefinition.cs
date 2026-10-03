using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Definizione immutabile di un oggetto (piano § 4.4). Chi la usa a runtime tiene un
    /// <see cref="ItemInstance"/>, che la riferisce con l'ID: un riferimento diretto a uno
    /// ScriptableObject non sopravvive a un salvataggio su file.
    /// </summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [Tooltip("Generato alla creazione e mai più cambiato: è quello che salvano le istanze. Duplicando l'asset va rigenerato.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [Tooltip("Icona per inventario e tooltip, generata dal modello (DarkDescent → Oggetti → Rigenera le icone).")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Celle occupate nell'inventario, larghezza × altezza.")]
        [SerializeField] private Vector2Int _size = Vector2Int.one;

        [Tooltip("Il modello: a terra, in mano e per l'icona.")]
        [SerializeField] private GameObject _model;

        [Tooltip("Rotazione del modello davanti alla camera dell'icona.")]
        [SerializeField] private Vector3 _iconRotation;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Vector2Int Size => _size;
        public GameObject Model => _model;
        public Vector3 IconRotation => _iconRotation;

        /// <summary>Lo slot in cui va equipaggiato; None per gli oggetti che non si indossano.</summary>
        public abstract EquipSlot Slot { get; }

        protected virtual void OnValidate()
        {
            EnsureId();
            _size = Vector2Int.Max(_size, Vector2Int.one);
        }

        // Reset scatta quando l'asset nasce dal menu Create, OnValidate quando si carica o si modifica
        private void Reset()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString("N");
            }
        }
    }
}
