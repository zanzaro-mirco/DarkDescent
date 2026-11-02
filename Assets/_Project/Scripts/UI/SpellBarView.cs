using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// La barra delle abilità accanto alla cintura (D4, D10 della M9), da tre posti a otto con la M20
    /// (D9). Ridisegna i posti quando un'abilità entra, esce, si sposta o si sceglie e quando il mana
    /// cambia di un punto; il velo della ricarica parte con il lancio e segue l'abilità se si sposta.
    /// Un click su un posto lo sceglie.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpellBarView : MonoBehaviour
    {
        [Tooltip("I posti, nell'ordine da F1 a F8.")]
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
            _magic.Book.Rearranged += HandleRearranged;
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
            _magic.Book.Rearranged -= HandleRearranged;
            _magic.Book.SelectionChanged -= HandleSlotChanged;
            _magic.Mana.Changed -= HandleManaChanged;
            _magic.Cast -= HandleCast;
            _subscribed = false;
        }

        private void HandleSlotChanged(int slot)
        {
            Refresh();
        }

        // la ricarica è dell'abilità: il velo riparte dove l'abilità è finita
        private void HandleRearranged()
        {
            Refresh();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_magic.Caster.CooldownFraction(i) > 0f)
                {
                    _slots[i].StartCooldown(_magic.Caster);
                }
            }
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
                var spell = book[i];
                bool known = book.Knows(i);
                bool affordable = spell != null && _magic.Mana.Current >= spell.ManaCost;
                _slots[i].Show(spell, known, i == book.Selected, affordable);
            }
        }
    }
}
