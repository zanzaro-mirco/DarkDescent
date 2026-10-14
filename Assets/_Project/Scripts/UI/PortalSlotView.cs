using DarkDescent.Magic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il posto della magia del portale, a sinistra della cintura (passo 11.7 della M11): l'icona, il
    /// tasto nell'angolo, scuro se il mana non basta. Un click, con qualsiasi tasto del mouse, lancia
    /// la magia come T. Si ridisegna sui cambi del mana.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortalSlotView : MonoBehaviour, IPointerDownHandler
    {
        [Tooltip("L'icona della magia, senza bersaglio dei raggi: la pressione la prende lo sfondo del posto.")]
        [SerializeField] private Image _icon;

        [Tooltip("Il colore dell'icona quando il mana non basta, come nella barra degli incantesimi.")]
        [SerializeField] private Color _unaffordable = new Color(0.35f, 0.35f, 0.45f, 1f);

        private PlayerMagic _magic;
        private bool _subscribed;

        public bool IsAffordable => _icon.color == Color.white;

        public bool HasIcon => _icon.enabled && _icon.sprite != null;

        /// <summary>Come la cintura: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerMagic magic)
        {
            Unsubscribe();
            _magic = magic;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_magic != null)
            {
                _magic.TryCastPortal();
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

            _magic.Mana.Changed += HandleManaChanged;
            _subscribed = true;
            _icon.sprite = _magic.PortalSpell != null ? _magic.PortalSpell.Icon : null;
            _icon.enabled = _icon.sprite != null;
            HandleManaChanged(_magic.Mana.Current, _magic.Mana.Max);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _magic.Mana.Changed -= HandleManaChanged;
            _subscribed = false;
        }

        private void HandleManaChanged(float current, float max)
        {
            bool affordable = _magic.PortalSpell != null && current >= _magic.PortalSpell.ManaCost;
            _icon.color = affordable ? Color.white : _unaffordable;
        }
    }
}
