using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Localization;
using UnityEngine;
using UnityEngine.Pool;

namespace DarkDescent.UI
{
    /// <summary>
    /// Numeri di danno fluttuanti sopra chi viene colpito. I numeri escono da un pool: nessun
    /// Instantiate né Destroy durante il combattimento, solo all'avvio e quando il pool cresce.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DamageNumbers : MonoBehaviour
    {
        [Tooltip("Numero modello, spento: il pool ne crea copie.")]
        [SerializeField] private DamageNumber _template;

        [SerializeField] private Color _enemyDamageColor = new Color(1f, 0.88f, 0.55f);
        [SerializeField] private Color _playerDamageColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color _missColor = new Color(0.7f, 0.7f, 0.7f);

        [Tooltip("Altezza sopra i piedi da cui parte il numero.")]
        [SerializeField, Min(0f)] private float _spawnHeight = 2.3f;

        [Tooltip("Di quanti metri sale il numero durante la sua vita.")]
        [SerializeField, Min(0f)] private float _rise = 1f;

        [SerializeField, Min(0.1f)] private float _lifetime = 0.8f;

        [Tooltip("Vuoto = Camera.main, risolta in Awake.")]
        [SerializeField] private Camera _camera;

        // per ogni Health seguita, i suoi handler: servono gli stessi delegati per il -=
        private readonly Dictionary<Health, (Action<DamageInfo, float> damaged, Action<DamageInfo> evaded)> _handlers =
            new Dictionary<Health, (Action<DamageInfo, float>, Action<DamageInfo>)>();
        private readonly List<DamageNumber> _active = new List<DamageNumber>();
        private ObjectPool<DamageNumber> _pool;
        private RectTransform _container;
        private Canvas _canvas;
        private Localizer _localizer;
        private bool _subscribed;

        public int ActiveCount => _active.Count;

        public int TrackedCount => _handlers.Count;

        private void Awake()
        {
            _container = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _template.gameObject.SetActive(false);
            _pool = new ObjectPool<DamageNumber>(
                createFunc: () => Instantiate(_template, _container),
                actionOnGet: number => number.gameObject.SetActive(true),
                actionOnRelease: number => number.gameObject.SetActive(false),
                actionOnDestroy: number => Destroy(number.gameObject),
                defaultCapacity: 8);
        }

        /// <summary>Le scritte come "Miss" si chiedono qui, nella lingua del momento.</summary>
        public void SetLocalizer(Localizer localizer)
        {
            _localizer = localizer;
        }

        /// <summary>
        /// Segue una vita: ogni danno mostra un numero sopra il suo proprietario. Come per la sfera,
        /// Track può arrivare prima o dopo OnEnable (trappola 12).
        /// </summary>
        public void Track(Health health, bool isPlayer)
        {
            if (health == null || _handlers.ContainsKey(health))
            {
                return;
            }

            Color color = isPlayer ? _playerDamageColor : _enemyDamageColor;
            Transform owner = health.transform;
            Action<DamageInfo, float> damaged = (info, applied) => Spawn(owner, applied, color);
            Action<DamageInfo> evaded = info => SpawnMiss(owner);
            _handlers.Add(health, (damaged, evaded));

            if (_subscribed)
            {
                Subscribe(health, damaged, evaded);
            }
        }

        /// <summary>
        /// Smette di seguire una vita, per esempio quando il suo livello viene scaricato.
        /// </summary>
        public void Untrack(Health health)
        {
            // ReferenceEquals e non ==: una Health distrutta è "null" per Unity, ma resta una chiave
            // valida del dizionario e va tolta lo stesso
            if (ReferenceEquals(health, null) || !_handlers.TryGetValue(health, out var handlers))
            {
                return;
            }

            if (_subscribed)
            {
                Unsubscribe(health, handlers.damaged, handlers.evaded);
            }

            _handlers.Remove(health);
        }

        private void OnEnable()
        {
            foreach (var pair in _handlers)
            {
                Subscribe(pair.Key, pair.Value.damaged, pair.Value.evaded);
            }

            _subscribed = true;
        }

        private void OnDisable()
        {
            // una Health distrutta (scheletro sparito) ha portato con sé i suoi delegati: il -= è innocuo
            foreach (var pair in _handlers)
            {
                Unsubscribe(pair.Key, pair.Value.damaged, pair.Value.evaded);
            }

            _subscribed = false;
        }

        private static void Subscribe(Health health, Action<DamageInfo, float> damaged, Action<DamageInfo> evaded)
        {
            health.Damaged += damaged;
            health.Evaded += evaded;
        }

        private static void Unsubscribe(Health health, Action<DamageInfo, float> damaged, Action<DamageInfo> evaded)
        {
            health.Damaged -= damaged;
            health.Evaded -= evaded;
        }

        private void Spawn(Transform owner, float amount, Color color)
        {
            var number = _pool.Get();
            number.Show(owner.position + Vector3.up * _spawnHeight, amount, color);
            Activate(number);
        }

        private void SpawnMiss(Transform owner)
        {
            var number = _pool.Get();
            number.ShowText(owner.position + Vector3.up * _spawnHeight, _localizer.Get(TextKeys.Miss), _missColor);
            Activate(number);
        }

        private void Activate(DamageNumber number)
        {
            _active.Add(number);
            // posizionato subito, non al prossimo Update: niente numero per un frame nell'angolo
            number.Tick(0f, _lifetime, _rise, _camera, _container, UiCamera);
        }

        // In Overlay la conversione da schermo a Canvas vuole null; in Screen Space Camera vuole la
        // camera della Canvas. Letta ogni volta: la modalità si può cambiare a runtime.
        private Camera UiCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        private void Update()
        {
            Camera uiCamera = UiCamera;

            // all'indietro: si può togliere dalla lista mentre la si scorre
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].Tick(Time.deltaTime, _lifetime, _rise, _camera, _container, uiCamera))
                {
                    _pool.Release(_active[i]);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
