using System.Collections;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.Player;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Un avviso al centro dello schermo, per un momento: "Non c'è posto nell'inventario" quando il
    /// cavaliere prova a raccogliere un oggetto che non entra (seconda prova della build M7), e dal
    /// passo 11.7 della M11 perché la magia del portale non parte, in città o nella sala del re, e
    /// dalla M13 il bonus di un santuario appena preso. Ascolta inventario, magia e bonus; un altro
    /// avviso la rimette a piena luce.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudMessage : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        [SerializeField] private CanvasGroup _group;

        [Tooltip("Secondi a piena luce prima di sfumare.")]
        [SerializeField, Min(0f)] private float _hold = 1.2f;

        [SerializeField, Min(0.05f)] private float _fade = 0.6f;

        private PlayerInventory _inventory;
        private PlayerMagic _magic;
        private ShrineBonus _bonus;
        private Localizer _localizer;
        private string _key;
        private bool _subscribed;
        private Coroutine _fading;

        /// <summary>Quanto si vede, per i test: 0 spenta.</summary>
        public float Alpha => _group.alpha;

        public string Text => _text.text;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInventory inventory, PlayerMagic magic, Localizer localizer, ShrineBonus bonus = null)
        {
            Unsubscribe();
            _inventory = inventory;
            _magic = magic;
            _bonus = bonus;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _group.alpha = 0f;
            _text.text = string.Empty;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _fading = null;
            _group.alpha = 0f;
        }

        private void Subscribe()
        {
            if (_subscribed || _inventory == null)
            {
                return;
            }

            _inventory.PickUpRefused += HandlePickUpRefused;
            if (_magic != null)
            {
                _magic.PortalRefused += HandlePortalRefused;
            }

            if (_bonus != null)
            {
                _bonus.Started += HandleBonusStarted;
            }

            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _inventory.PickUpRefused -= HandlePickUpRefused;
            if (_magic != null)
            {
                _magic.PortalRefused -= HandlePortalRefused;
            }

            if (_bonus != null)
            {
                _bonus.Started -= HandleBonusStarted;
            }

            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        private void HandlePickUpRefused(ItemInstance item)
        {
            Show(TextKeys.InventoryFull);
        }

        // senza mana il posto del portale è già scuro, e il suono sordo basta: come gli altri incantesimi
        private void HandlePortalRefused(PortalResult result)
        {
            if (result == PortalResult.InTown)
            {
                Show(TextKeys.PortalInTown);
            }
            else if (result == PortalResult.BossRoom)
            {
                Show(TextKeys.PortalBossRoom);
            }
        }

        private void HandleBonusStarted(ShrineDefinition shrine)
        {
            Show(shrine.MessageKey);
        }

        /// <summary>Una scritta che sfuma, dalla sua chiave: anche "Il re è caduto" (D15 della M14).</summary>
        public void Show(string key)
        {
            _key = key;
            Refresh();
            if (_fading != null)
            {
                StopCoroutine(_fading);
            }

            _group.alpha = 1f;
            _fading = StartCoroutine(Fade());
        }

        // tempo reale, come la scritta del livello: l'hit stop non la ferma
        private IEnumerator Fade()
        {
            yield return new WaitForSecondsRealtime(_hold);
            for (float time = 0f; time < _fade; time += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - time / _fade;
                yield return null;
            }

            _group.alpha = 0f;
            _fading = null;
        }

        private void Refresh()
        {
            _text.text = _localizer != null && _key != null ? _localizer.Get(_key) : string.Empty;
        }
    }
}
