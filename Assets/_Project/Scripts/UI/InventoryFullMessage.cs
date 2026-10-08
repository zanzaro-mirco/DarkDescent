using System.Collections;
using DarkDescent.Items;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// "Non c'è posto nell'inventario" al centro dello schermo, per un momento, quando il cavaliere
    /// prova a raccogliere un oggetto che non entra (seconda prova della build M7). Prima lo diceva
    /// l'etichetta dell'oggetto, e restava lì. Ascolta l'inventario; un altro tentativo la rimette a
    /// piena luce.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryFullMessage : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        [SerializeField] private CanvasGroup _group;

        [Tooltip("Secondi a piena luce prima di sfumare.")]
        [SerializeField, Min(0f)] private float _hold = 1.2f;

        [SerializeField, Min(0.05f)] private float _fade = 0.6f;

        private PlayerInventory _inventory;
        private Localizer _localizer;
        private bool _subscribed;
        private Coroutine _fading;

        /// <summary>Quanto si vede, per i test: 0 spenta.</summary>
        public float Alpha => _group.alpha;

        public string Text => _text.text;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInventory inventory, Localizer localizer)
        {
            Unsubscribe();
            _inventory = inventory;
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

            _inventory.PickUpRefused += Show;
            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _inventory.PickUpRefused -= Show;
            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        private void Show(ItemInstance item)
        {
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
            _text.text = _localizer != null ? _localizer.Get(TextKeys.InventoryFull) : string.Empty;
        }
    }
}
