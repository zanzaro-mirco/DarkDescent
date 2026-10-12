using System;
using System.Collections;
using System.Collections.Generic;
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
    /// in Diablo, così si sa chi si sta per colpire. Senza un nemico sotto il cursore mostra quello che
    /// il cavaliere ha colpito per ultimo, di mischia o con un incantesimo, per qualche secondo dopo il
    /// colpo (D9 della M9): con i proiettili si colpisce senza il cursore sopra. Ascolta il controller
    /// del cavaliere e i nemici del livello; sparisce quando non c'è nessuno da mostrare.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBar : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;

        [SerializeField] private TMP_Text _name;

        [Tooltip("La barra della vita: si accorcia da destra spostando il suo bordo destro, senza sprite.")]
        [SerializeField] private RectTransform _fill;

        [Tooltip("Per quanti secondi dopo l'ultimo colpo resta il nemico combattuto.")]
        [SerializeField, Min(0f)] private float _lastHitDuration = 3f;

        private PlayerController _controller;
        private Localizer _localizer;
        private Health _enemy;
        private string _nameKey;
        private bool _subscribed;

        // i nemici seguiti: il colpo e lo schivato di ognuno, per sapere chi il cavaliere sta combattendo
        private readonly Dictionary<Health, (Action<DamageInfo, float> damaged, Action<DamageInfo> evaded)> _tracked =
            new Dictionary<Health, (Action<DamageInfo, float>, Action<DamageInfo>)>();

        private Health _hovered;
        private Health _pinned;
        private Health _lastHit;
        private Coroutine _lastHitTimer;
        private WaitForSeconds _lastHitWait;

        /// <summary>Il nemico mostrato, per i test; null se la barra è nascosta.</summary>
        public Health Shown => _enemy;

        /// <summary>Il nemico colpito per ultimo dal cavaliere, finché non scade; null se non c'è.</summary>
        public Health LastHit => _lastHit;

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

        /// <summary>Segue un nemico del livello: chi lo colpisce, se è il cavaliere, lo rende il nemico combattuto.</summary>
        public void Track(Health enemy)
        {
            if (enemy == null || _tracked.ContainsKey(enemy))
            {
                return;
            }

            Action<DamageInfo, float> damaged = (info, applied) => HandleStruck(enemy, info);
            Action<DamageInfo> evaded = info => HandleStruck(enemy, info);
            enemy.Damaged += damaged;
            enemy.Evaded += evaded;
            _tracked.Add(enemy, (damaged, evaded));
        }

        /// <summary>
        /// Tiene in alto un nemico finché non lo si lascia o muore: il boss, per tutto lo scontro (D8
        /// della M10). Il nemico sotto il cursore passa comunque davanti.
        /// </summary>
        public void Pin(Health enemy)
        {
            _pinned = enemy;
            Refresh();
        }

        public void Unpin()
        {
            _pinned = null;
            Refresh();
        }

        /// <summary>Il nemico tenuto in alto, per i test; null se nessuno.</summary>
        public Health Pinned => _pinned;

        /// <summary>Lascia un nemico: il livello se ne va, anche con i nemici già distrutti.</summary>
        public void Untrack(Health enemy)
        {
            // ReferenceEquals: una Health distrutta è "null" per Unity ma resta una chiave da togliere
            if (ReferenceEquals(enemy, null) || !_tracked.TryGetValue(enemy, out var handlers))
            {
                return;
            }

            enemy.Damaged -= handlers.damaged;
            enemy.Evaded -= handlers.evaded;
            _tracked.Remove(enemy);
            if (ReferenceEquals(_pinned, enemy))
            {
                _pinned = null;
            }

            if (ReferenceEquals(_lastHit, enemy))
            {
                ForgetLastHit();
            }
        }

        private void Awake()
        {
            _panel.SetActive(false);
            _lastHitWait = new WaitForSeconds(_lastHitDuration);
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

            _controller.HoveredEnemyChanged += HandleHovered;
            _localizer.LanguageChanged += RefreshName;
            _subscribed = true;
            HandleHovered(_controller.HoveredEnemy);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _controller.HoveredEnemyChanged -= HandleHovered;
            _localizer.LanguageChanged -= RefreshName;
            _subscribed = false;
            _hovered = null;
            ForgetLastHit();
        }

        private void HandleHovered(Health enemy)
        {
            _hovered = enemy;
            Refresh();
        }

        // solo i colpi del cavaliere, di mischia o con un incantesimo: un nemico non combatte gli altri
        private void HandleStruck(Health enemy, DamageInfo info)
        {
            if (_controller == null || info.Source != _controller.gameObject || enemy.IsDead)
            {
                return;
            }

            _lastHit = enemy;
            if (_lastHitTimer != null)
            {
                StopCoroutine(_lastHitTimer);
            }

            _lastHitTimer = isActiveAndEnabled ? StartCoroutine(ExpireLastHit()) : null;
            Refresh();
        }

        private IEnumerator ExpireLastHit()
        {
            yield return _lastHitWait;
            _lastHitTimer = null;
            _lastHit = null;
            Refresh();
        }

        private void ForgetLastHit()
        {
            if (_lastHitTimer != null)
            {
                StopCoroutine(_lastHitTimer);
                _lastHitTimer = null;
            }

            _lastHit = null;
            Refresh();
        }

        // il cursore vince sul nemico combattuto; morto, il combattuto si dimentica
        private void Refresh()
        {
            if (_lastHit != null && _lastHit.IsDead)
            {
                _lastHit = null;
            }

            if (_pinned != null && _pinned.IsDead)
            {
                _pinned = null;
            }

            Show(_hovered != null && !_hovered.IsDead ? _hovered : _pinned != null ? _pinned : _lastHit);
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
            Refresh();
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
