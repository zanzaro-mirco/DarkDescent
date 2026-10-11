using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// La barra dei tre incantesimi accanto alla cintura (D4, D10 della M9). Ridisegna i posti
    /// quando si impara o si sceglie un incantesimo e quando il mana cambia di un punto; il velo
    /// della ricarica parte con il lancio. Un click su un posto lo sceglie.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpellBarView : MonoBehaviour
    {
        [Tooltip("I posti, nell'ordine F1, F2, F3.")]
        [SerializeField] private SpellSlotView[] _slots;

        private PlayerMagic _magic;
        private bool _subscribed;

        public SpellSlotView Slot(int index) => _slots[index];

        public void Bind(PlayerMagic magic)
        {
            Unsubscribe();
            _magic = magic;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Il click su un posto: lo sceglie, se l'incantesimo è noto.</summary>
        public void ClickSlot(int slot)
        {
            _magic?.Select(slot);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _magic == null)
            {
                return;
            }

            _magic.Book.Learned += HandleSlotChanged;
            _magic.Book.SelectionChanged += HandleSlotChanged;
            _magic.Mana.Changed += HandleManaChanged;
            _magic.Cast += HandleCast;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _magic.Book.Learned -= HandleSlotChanged;
            _magic.Book.SelectionChanged -= HandleSlotChanged;
            _magic.Mana.Changed -= HandleManaChanged;
            _magic.Cast -= HandleCast;
            _subscribed = false;
        }

        private void HandleSlotChanged(int slot)
        {
            Refresh();
        }

        private void HandleManaChanged(float current, float max)
        {
            Refresh();
        }

        private void HandleCast(SpellDefinition spell, Vector3 target)
        {
            int slot = _magic.Book.SlotOf(spell);
            if (slot >= 0 && slot < _slots.Length && spell.Cooldown > 0f)
            {
                _slots[slot].StartCooldown(_magic.Caster);
            }
        }

        private void Refresh()
        {
            var book = _magic.Book;
            for (int i = 0; i < _slots.Length; i++)
            {
                var spell = i < book.Count ? book[i] : null;
                bool known = book.Knows(i);
                bool affordable = spell != null && _magic.Mana.Current >= spell.ManaCost;
                _slots[i].Show(spell, known, i == book.Selected, affordable);
            }
        }
    }
}
