using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un affisso tirato su un oggetto: l'ID della definizione e il valore uscito (D6 della M5).
    /// Si salva il valore e non solo il seme: se un giorno si ritarano gli intervalli, gli oggetti
    /// già trovati restano quelli.
    /// </summary>
    [Serializable]
    public sealed class ItemAffix
    {
        [SerializeField] private string _affixId;
        [SerializeField] private int _value;

        // non serializzata: dopo un caricamento si ritrova con Resolve
        [NonSerialized] private AffixDefinition _definition;

        public ItemAffix(AffixDefinition definition, int value)
        {
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _affixId = definition.Id;
            _value = value;
        }

        public string AffixId => _affixId;

        public int Value => _value;

        /// <summary>La definizione; null per un affisso appena letto da file e non ancora risolto.</summary>
        public AffixDefinition Definition => _definition;

        public bool Resolve(AffixDatabase database)
        {
            return database.TryGet(_affixId, out _definition);
        }
    }
}
