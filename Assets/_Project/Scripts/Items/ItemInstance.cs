using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un oggetto preciso: quello in inventario, a terra o in mano. Classe semplice e serializzabile
    /// che riferisce la definizione con l'ID, così alla M8 si salva in JSON così com'è. Alla M5
    /// porterà anche affissi e seme.
    /// </summary>
    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField] private string _definitionId;

        // non serializzata: dopo un caricamento si ritrova con Resolve
        [NonSerialized] private ItemDefinition _definition;

        public ItemInstance(ItemDefinition definition)
        {
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _definitionId = definition.Id;
        }

        public string DefinitionId => _definitionId;

        /// <summary>La definizione; null per un'istanza appena letta da file e non ancora risolta.</summary>
        public ItemDefinition Definition => _definition;

        /// <summary>Ritrova la definizione dall'ID; false se il database non la conosce.</summary>
        public bool Resolve(ItemDatabase database)
        {
            return database.TryGet(_definitionId, out _definition);
        }
    }
}
