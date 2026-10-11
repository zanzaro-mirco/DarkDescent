using System.Collections;
using DarkDescent.Magic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Un posto della barra degli incantesimi (D4, D10 della M9): l'icona se l'incantesimo è noto,
    /// la cornice d'oro se è quello scelto, scuro se il mana non basta, e un velo che si ritira
    /// durante la ricarica. La pressione la inoltra alla barra.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpellSlotView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private SpellBarView _bar;

        [SerializeField, Min(0)] private int _slot;

        [Tooltip("L'icona dell'incantesimo, senza bersaglio dei raggi.")]
        [SerializeField] private Image _icon;

        [Tooltip("La cornice del posto scelto, spenta a riposo.")]
        [SerializeField] private Image _selectedFrame;

        [Tooltip("Il velo della ricarica: Filled radiale, dall'alto, si ritira fino a sparire.")]
        [SerializeField] private Image _cooldown;

        [Tooltip("Il colore dell'icona quando il mana non basta.")]
        [SerializeField] private Color _unaffordable = new Color(0.35f, 0.35f, 0.45f, 1f);

        private Coroutine _cooling;

        public int Index => _slot;

        public bool HasIcon => _icon.enabled;

        public bool IsSelected => _selectedFrame.enabled;

        public bool IsAffordable => _icon.color == Color.white;

        public float CooldownFill => _cooldown.enabled ? _cooldown.fillAmount : 0f;

        public void Show(SpellDefinition spell, bool known, bool selected, bool affordable)
        {
            _icon.sprite = spell != null ? spell.Icon : null;
            _icon.enabled = known && spell != null;
            _icon.color = affordable ? Color.white : _unaffordable;
            _selectedFrame.enabled = known && selected;
        }

        /// <summary>Il velo segue la ricarica del lancio appena fatto, fino a sparire.</summary>
        public void StartCooldown(SpellCaster caster)
        {
            if (_cooling != null)
            {
                StopCoroutine(_cooling);
            }

            _cooling = isActiveAndEnabled ? StartCoroutine(FollowCooldown(caster)) : null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _bar.ClickSlot(_slot);
        }

        private void OnDisable()
        {
            _cooldown.enabled = false;
            _cooling = null;
        }

        // si legge la ricarica solo mentre c'è: il velo è un'animazione del lancio, non un controllo continuo
        private IEnumerator FollowCooldown(SpellCaster caster)
        {
            _cooldown.enabled = true;
            for (float left = caster.CooldownFraction(_slot); left > 0f; left = caster.CooldownFraction(_slot))
            {
                _cooldown.fillAmount = left;
                yield return null;
            }

            _cooldown.enabled = false;
            _cooling = null;
        }
    }
}
