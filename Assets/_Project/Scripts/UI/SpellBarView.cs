using DarkDescent.Localization;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Le quattro caselle alla Diablo 3 nella console in basso (D18 della M20): il sinistro, il
    /// destro, Q ed E. Ridisegna le caselle quando un'abilità entra, esce o si sposta e quando il mana
    /// cambia di un punto; il velo della ricarica parte con il lancio e segue l'abilità se si sposta.
    /// Un click su una casella apre il menu delle abilità su di lei. Fino al 20.4 i posti erano otto,
    /// da F1 a F8, e il click sceglieva quello da lanciare con il destro.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpellBarView : MonoBehaviour
    {
        /// <summary>Le chiavi dei tasti scritti sotto le caselle, nell'ordine delle caselle.</summary>
        public static readonly string[] KeyKeys = { "skills.key.primary", "skills.key.secondary", "skills.key.q", "skills.key.e" };

        [Tooltip("Le caselle, nell'ordine: sinistro, destro, Q, E.")]
        [SerializeField] private SpellSlotView[] _slots;

        [Tooltip("Il menu delle abilità, aperto da un click su una casella.")]
        [SerializeField] private SkillMenu _menu;

        private PlayerMagic _magic;
        private Localizer _localizer;
        private bool _subscribed;

        public SpellSlotView Slot(int index) => _slots[index];

        public int Count => _slots.Length;

        public SkillMenu Menu => _menu;

        public void Bind(PlayerMagic magic, Localizer localizer)
        {
            Unsubscribe();
            _magic = magic;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Il click su una casella: apre il menu delle abilità su di lei, o lo chiude.</summary>
        public void ClickSlot(int slot)
        {
            if (_menu != null)
            {
                _menu.ToggleSlot(slot);
            }
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

            _magic.Book.Learned += HandleLearned;
            _magic.Book.Rearranged += HandleRearranged;
            _magic.Mana.Changed += HandleManaChanged;
            _magic.Cast += HandleCast;
            if (_localizer != null)
            {
                _localizer.LanguageChanged += Refresh;
            }

            if (_menu != null)
            {
                _menu.Changed += Refresh;
            }

            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _magic.Book.Learned -= HandleLearned;
            _magic.Book.Rearranged -= HandleRearranged;
            _magic.Mana.Changed -= HandleManaChanged;
            _magic.Cast -= HandleCast;
            if (_localizer != null)
            {
                _localizer.LanguageChanged -= Refresh;
            }

            if (_menu != null)
            {
                _menu.Changed -= Refresh;
            }

            _subscribed = false;
        }

        private void HandleLearned(int slot)
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
                bool affordable = spell != null && _magic.Mana.Current >= _magic.Caster.ManaCost(spell);
                _slots[i].Show(spell, SkillMenu.NameOf(spell, _localizer), affordable);
                _slots[i].SetSelected(_menu != null && _menu.IsOpen && _menu.ShowsSlot(i));
                if (_localizer != null && i < KeyKeys.Length)
                {
                    _slots[i].SetKey(_localizer.Get(KeyKeys[i]));
                }
            }
        }
    }
}
