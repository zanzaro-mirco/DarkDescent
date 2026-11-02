using System.Collections;
using DarkDescent.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Una delle quattro caselle alla Diablo 3 (D18 della M20): l'icona dell'abilità, o il suo nome
    /// finché le icone non ci sono (20.7); scura se il mana non basta; un velo che si ritira durante la
    /// ricarica; il tasto scritto sotto; la cornice accesa mentre il menu delle abilità è aperto su di
    /// lei. Un click la inoltra alla barra, che apre il menu.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpellSlotView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private SpellBarView _bar;

        [SerializeField, Min(0)] private int _slot;

        [Tooltip("L'icona dell'abilità, senza bersaglio dei raggi.")]
        [SerializeField] private Image _icon;

        [Tooltip("Il nome dell'abilità, quando non ha un'icona.")]
        [SerializeField] private TMP_Text _name;

        [Tooltip("Il tasto della casella: sinistro, destro, Q, E.")]
        [SerializeField] private TMP_Text _key;

        [Tooltip("La cornice della casella che il menu sta cambiando, spenta a riposo.")]
        [SerializeField] private Image _selectedFrame;

        [Tooltip("Il velo della ricarica: Filled radiale, dall'alto, si ritira fino a sparire.")]
        [SerializeField] private Image _cooldown;

        [Tooltip("Il colore dell'icona quando il mana non basta.")]
        [SerializeField] private Color _unaffordable = new Color(0.35f, 0.35f, 0.45f, 1f);

        private Coroutine _cooling;

        public int Index => _slot;

        public bool HasIcon => _icon.enabled;

        /// <summary>Se la casella mostra un'abilità, con l'icona o con il nome.</summary>
        public bool IsFilled => _icon.enabled || (_name != null && _name.enabled);

        public string ShownName => _name != null && _name.enabled ? _name.text : "";

        public string KeyText => _key != null ? _key.text : "";

        public bool IsSelected => _selectedFrame.enabled;

        public bool IsAffordable => _icon.color == Color.white;

        public float CooldownFill => _cooldown.enabled ? _cooldown.fillAmount : 0f;

        public void Show(SpellDefinition spell, string name, bool affordable)
        {
            var color = affordable ? Color.white : _unaffordable;
            _icon.sprite = spell != null ? spell.Icon : null;
            _icon.enabled = spell != null && spell.Icon != null;
            _icon.color = color;
            if (_name != null)
            {
                _name.text = name;
                _name.enabled = spell != null && spell.Icon == null;
                _name.color = color;
            }
        }

        public void SetKey(string key)
        {
            if (_key != null)
            {
                _key.text = key;
            }
        }

        public void SetSelected(bool selected)
        {
            _selectedFrame.enabled = selected;
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
