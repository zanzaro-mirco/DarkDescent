using System;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.Player
{
    /// <summary>
    /// Collega input, movimento e attacco: ascolta il reader, guarda cosa c'è sotto il cursore e
    /// comanda il motor (terreno, oggetti da usare) o l'attacco (nemico). È l'unico a conoscere
    /// camera, layer e mondo 3D. Ogni frame guarda anche cosa c'è sotto il cursore, per evidenziarlo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(MeleeAttack))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Layer su cui un click fa camminare.")]
        [SerializeField] private LayerMask _walkableLayers;

        [Tooltip("Layer degli ostacoli: muri, colonne, barili, muri bassi. Un click su di essi non fa nulla, ma il raggio li attraversa, tranne i muri alti (tag Wall), per arrivare a un nemico o a una cassa dietro.")]
        [SerializeField] private LayerMask _blockingLayers;

        [Tooltip("Layer dei nemici: un click su di essi attacca.")]
        [SerializeField] private LayerMask _enemyLayers;

        [Tooltip("Layer delle cose da usare (scale, poi porte e oggetti): un click ci porta il player.")]
        [SerializeField] private LayerMask _interactableLayers;

        [SerializeField, Min(1f)] private float _maxRayDistance = 100f;

        [Tooltip("Un click sul pavimento a meno di tanti metri da un nemico vivo colpisce lui (prova della M7: lo sciame è piccolo e fitto, e il click finiva a terra).")]
        [SerializeField, Min(0f)] private float _enemyClickTolerance = 0.6f;

        [Tooltip("A che distanza, in orizzontale, dal punto d'arrivo di un oggetto cliccato lo si usa (raccogliere, aprire).")]
        [SerializeField, Min(0.1f)] private float _useReach = 1f;

        [Tooltip("Ogni quanti secondi si aggiorna il comando tenendo premuto il tasto.")]
        [SerializeField, Min(0.02f)] private float _holdRepathInterval = 0.1f;

        [Tooltip("Vuoto = Camera.main, risolta una volta in Awake.")]
        [SerializeField] private Camera _camera;

        /// <summary>Il tag dei muri alti: fermano il raggio del cursore, gli altri ostacoli no.</summary>
        public const string WallTag = "Wall";

        // i collider sotto il cursore, dal più vicino: riusato a ogni frame, niente allocazioni
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        // i nemici attorno al punto del pavimento cliccato
        private readonly Collider[] _nearby = new Collider[8];

        private PlayerMotor _motor;
        private PlayerInputReader _input;
        private MeleeAttack _attack;
        private float _holdTimer;

        // nemico cliccato all'inizio della pressione: tenendo premuto si continua a colpire lui,
        // ovunque vada il cursore, come in Diablo
        private IDamageable _heldTarget;

        // movimento chiesto durante un colpo, o mentre un colpo forte tiene fermo il cavaliere (D7 della
        // M7): parte appena il colpo è arrivato o il blocco è finito
        private bool _hasQueuedMove;
        private Vector3 _queuedMove;

        // oggetto cliccato verso cui si sta andando: raggiunto, lo si usa
        private Interactable _pendingUse;

        // La pressione arriva da una callback dell'Input System, dove IsPointerOverGameObject dà lo
        // stato della UI del frame prima e genera un warning (trappola 7): la si annota e la si
        // esegue nel primo Update. Una pressione nata sopra la UI non diventa mai un movimento.
        private bool _pressPending;
        private bool _pressStartedOverUI;

        /// <summary>L'oggetto da usare sotto il cursore è cambiato; null = nessuno.</summary>
        public event Action<Interactable> HoveredChanged;

        public Interactable Hovered { get; private set; }

        /// <summary>Il nemico vivo sotto il cursore è cambiato; null = nessuno. L'HUD ne mostra nome e vita.</summary>
        public event Action<Health> HoveredEnemyChanged;

        public Health HoveredEnemy { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
            _attack = GetComponent<MeleeAttack>();

            // Camera.main è una ricerca per tag: una volta qui, mai per frame
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            _input.MoveCommandStarted += HandleMoveCommandStarted;
            _input.MoveCommandCanceled += HandleMoveCommandCanceled;
        }

        private void OnDisable()
        {
            _input.MoveCommandStarted -= HandleMoveCommandStarted;
            _input.MoveCommandCanceled -= HandleMoveCommandCanceled;

            // spento il controller (morte, cambio di livello) niente resta evidenziato né da usare
            SetHovered(null);
            SetHoveredEnemy(null);
            _pendingUse = null;
        }

        private void Update()
        {
            UpdateHover();
            UpdatePendingUse();

            if (_hasQueuedMove && !IsBusy)
            {
                _hasQueuedMove = false;
                Walk(_queuedMove);
            }

            if (_pressPending)
            {
                _pressPending = false;
                ExecutePress();
                return;
            }

            if (!_input.IsMoveCommandHeld || _pressStartedOverUI)
            {
                return;
            }

            _holdTimer -= Time.deltaTime;
            if (_holdTimer > 0f)
            {
                return;
            }

            _holdTimer = _holdRepathInterval;

            if (_heldTarget != null)
            {
                // morto il bersaglio tenuto, ci si ferma finché non si rilascia: niente passi a sorpresa
                if (_heldTarget.IsAlive())
                {
                    _attack.SetTarget(_heldTarget);
                }

                return;
            }

            // tenendo premuto e passando sopra un nemico non si cambia idea: si continua a camminare
            ExecuteCursorCommand(allowAttack: false);
        }

        private void HandleMoveCommandStarted()
        {
            _pressPending = true;
        }

        private void ExecutePress()
        {
            _holdTimer = _holdRepathInterval;
            _heldTarget = null;
            _pressStartedOverUI = IsPointerOverUI();
            if (!_pressStartedOverUI)
            {
                ExecuteCursorCommand(allowAttack: true);
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleMoveCommandCanceled()
        {
            _heldTarget = null;
        }

        private void ExecuteCursorCommand(bool allowAttack)
        {
            if (!TryRaycastCursor(out RaycastHit hit))
            {
                return;
            }

            int layerBit = 1 << hit.collider.gameObject.layer;
            // la tolleranza vale alla pressione: tenendo premuto e passando vicino a un nemico si
            // continua a camminare dove punta il cursore
            var enemy = allowAttack || (_enemyLayers.value & layerBit) != 0 ? EnemyUnderCursor(hit) : null;

            if (enemy != null)
            {
                if (!allowAttack)
                {
                    return;
                }

                IDamageable target = enemy;
                if (target.IsAlive())
                {
                    _heldTarget = target;
                    _hasQueuedMove = false;
                    _pendingUse = null;
                    _attack.SetTarget(target);
                }

                return;
            }

            if ((_interactableLayers.value & layerBit) != 0)
            {
                // come per i nemici, solo alla pressione: tenendo premuto sopra le scale si continua
                // a camminare dove si stava andando
                var interactable = hit.collider.GetComponentInParent<Interactable>();
                if (allowAttack && interactable != null)
                {
                    RequestWalk(interactable.ApproachPoint);
                    _pendingUse = interactable;
                }

                return;
            }

            if ((_walkableLayers.value & layerBit) != 0)
            {
                RequestWalk(hit.point);
            }
        }

        // Lo stesso raggio del click: un muro alto davanti alla scala la nasconde anche al cursore
        private void UpdateHover()
        {
            Interactable hovered = null;
            Health enemy = null;
            if (!IsPointerOverUI() && TryRaycastCursor(out RaycastHit hit))
            {
                if ((_interactableLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    hovered = hit.collider.GetComponentInParent<Interactable>();
                }
                else
                {
                    enemy = EnemyUnderCursor(hit);
                }
            }

            SetHovered(hovered);
            SetHoveredEnemy(enemy);
        }

        // Il nemico vivo colpito dal raggio, oppure, se il raggio è finito sul pavimento, il nemico vivo
        // più vicino al punto entro la tolleranza; null se non ce n'è.
        private Health EnemyUnderCursor(RaycastHit hit)
        {
            int layerBit = 1 << hit.collider.gameObject.layer;
            if ((_enemyLayers.value & layerBit) != 0)
            {
                // il collider può stare su un figlio del nemico: si risale fino a chi riceve i colpi
                var health = hit.collider.GetComponentInParent<Health>();
                return health != null && !health.IsDead ? health : null;
            }

            if ((_walkableLayers.value & layerBit) == 0 || _enemyClickTolerance <= 0f)
            {
                return null;
            }

            int count = Physics.OverlapSphereNonAlloc(hit.point, _enemyClickTolerance, _nearby, _enemyLayers, QueryTriggerInteraction.Ignore);
            Health nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var health = _nearby[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                {
                    continue;
                }

                Vector3 offset = health.transform.position - hit.point;
                offset.y = 0f;
                if (offset.sqrMagnitude < best)
                {
                    best = offset.sqrMagnitude;
                    nearest = health;
                }
            }

            return nearest;
        }

        private void SetHoveredEnemy(Health enemy)
        {
            // ReferenceEquals: come per gli oggetti, un nemico distrutto deve comunque dare null a chi ascolta
            if (ReferenceEquals(enemy, HoveredEnemy))
            {
                return;
            }

            HoveredEnemy = enemy;
            HoveredEnemyChanged?.Invoke(enemy);
        }

        private void SetHovered(Interactable hovered)
        {
            // ReferenceEquals: se quello di prima è stato distrutto, null deve comunque arrivare a chi ascolta
            if (ReferenceEquals(hovered, Hovered))
            {
                return;
            }

            // quello di prima può essere già distrutto, con la sua scena
            if (Hovered != null)
            {
                Hovered.SetHighlighted(false);
            }

            Hovered = hovered;
            if (hovered != null)
            {
                hovered.SetHighlighted(true);
            }

            HoveredChanged?.Invoke(hovered);
        }

        private void UpdatePendingUse()
        {
            // l'oggetto può sparire nel frattempo: raccolto, o con il suo livello
            if (_pendingUse == null)
            {
                _pendingUse = null;
                return;
            }

            Vector3 offset = _pendingUse.ApproachPoint - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > _useReach * _useReach)
            {
                return;
            }

            Interactable target = _pendingUse;
            _pendingUse = null;
            target.Use(gameObject);
        }

        private void RequestWalk(Vector3 point)
        {
            // un comando nuovo dimentica l'oggetto che si stava andando a usare
            _pendingUse = null;

            // il colpo partito si finisce, e un colpo forte subito tiene fermi: il movimento resta in
            // coda e parte subito dopo
            if (IsBusy)
            {
                _queuedMove = point;
                _hasQueuedMove = true;
                return;
            }

            _hasQueuedMove = false;
            Walk(point);
        }

        private bool IsBusy => _attack.IsSwinging || _attack.IsInterrupted;

        private void Walk(Vector3 point)
        {
            _attack.ClearTarget();
            _motor.MoveTo(point);
        }

        // Il raggio usa camminabili + bloccanti + nemici + interagibili e decide sul primo collider
        // colpito. Con una sola eccezione (prova della M7): se il primo è un ostacolo basso (colonna,
        // barile, muro basso) e dietro c'è un nemico o una cosa da usare, vince quello, così la cassa o
        // lo scheletro dietro una colonna si cliccano. Un muro alto o il pavimento fermano la ricerca:
        // niente click in un'altra stanza. Un click sulla sola colonna, come prima, non fa nulla.
        private bool TryRaycastCursor(out RaycastHit hit)
        {
            hit = default;
            if (_camera == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(_input.PointerScreenPosition);
            int mask = _walkableLayers | _blockingLayers | _enemyLayers | _interactableLayers;
            int count = Physics.RaycastNonAlloc(ray, _hits, _maxRayDistance, mask, QueryTriggerInteraction.Ignore);
            if (count == 0)
            {
                return false;
            }

            SortByDistance(count);
            int targets = _enemyLayers | _interactableLayers;
            for (int i = 0; i < count; i++)
            {
                var collider = _hits[i].collider;
                int layerBit = 1 << collider.gameObject.layer;
                if ((targets & layerBit) != 0)
                {
                    hit = _hits[i];
                    return true;
                }

                bool lowObstacle = (_blockingLayers.value & layerBit) != 0 && !collider.CompareTag(WallTag);
                if (!lowObstacle)
                {
                    break;
                }
            }

            hit = _hits[0];
            return true;
        }

        // RaycastNonAlloc non ordina: pochi elementi, un ordinamento per inserimento senza allocare
        private void SortByDistance(int count)
        {
            for (int i = 1; i < count; i++)
            {
                var current = _hits[i];
                int j = i - 1;
                while (j >= 0 && _hits[j].distance > current.distance)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }

                _hits[j + 1] = current;
            }
        }
    }
}
