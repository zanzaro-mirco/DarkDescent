using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Localization;
using DarkDescent.Player;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il nemico sotto il cursore (prova della M7): in alto al centro il suo nome e la sua vita, come
    /// in Diablo, così si sa chi si sta per colpire. Ascolta il controller del cavaliere e la vita del
    /// nemico mostrato; sparisce quando il cursore lo lascia o quando muore.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBar : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;

        [SerializeField] private TMP_Text _name;

        [Tooltip("La barra della vita: si accorcia da destra spostando il suo bordo destro, senza sprite.")]
        [SerializeField] private RectTransform _fill;

        private PlayerController _controller;
        private Localizer _localizer;
        private Health _enemy;
        private string _nameKey;
        private bool _subscribed;

        /// <summary>Il nemico mostrato, per i test; null se la barra è nascosta.</summary>
        public Health Shown => _enemy;

        public string NameText => _name.text;

        public float FillAmount => _fill.anchorMax.x;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerController controller, Localizer localizer)
        {
            Unsubscribe();
            _controller = controller;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _panel.SetActive(false);
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
            if (_subscribed || _controller == null)
            {
                return;
            }

            _controller.HoveredEnemyChanged += Show;
            _localizer.LanguageChanged += RefreshName;
            _subscribed = true;
            Show(_controller.HoveredEnemy);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _controller.HoveredEnemyChanged -= Show;
            _localizer.LanguageChanged -= RefreshName;
            _subscribed = false;
            Show(null);
        }

        private void Show(Health enemy)
        {
            // il nemico di prima può essere già distrutto con il suo livello: ci si stacca lo stesso
            if (!ReferenceEquals(_enemy, null))
            {
                _enemy.HealthChanged -= HandleHealthChanged;
                _enemy.Died -= Hide;
            }

            _enemy = enemy != null && !enemy.IsDead ? enemy : null;
            if (_enemy == null)
            {
                _panel.SetActive(false);
                return;
            }

            _enemy.HealthChanged += HandleHealthChanged;
            _enemy.Died += Hide;
            _nameKey = _enemy.TryGetComponent(out EnemyAI ai) && ai.Archetype != null ? ai.Archetype.NameKey : null;
            RefreshName();
            HandleHealthChanged(_enemy.Current, _enemy.Max);
            _panel.SetActive(true);
        }

        private void Hide()
        {
            Show(null);
        }

        private void HandleHealthChanged(float current, float max)
        {
            _fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        private void RefreshName()
        {
            _name.text = string.IsNullOrEmpty(_nameKey) || _localizer == null ? string.Empty : _localizer.Get(_nameKey);
        }
    }
}
