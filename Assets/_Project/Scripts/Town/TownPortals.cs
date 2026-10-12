using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Town
{
    /// <summary>
    /// I portali della pergamena del ritorno (D6 della M10), come il <i>Town Portal</i> di Diablo 1.
    /// Letta nel dungeon, la pergamena apre un portale davanti al cavaliere che porta all'accampamento;
    /// arrivati, accanto all'ingresso aspetta un portale che riporta nel punto in cui si era aperto,
    /// nella profondità rigenerata dal seme (ADR-061). Riattraversato, si chiude. Profondità e punto del
    /// ritorno vanno nel salvataggio (D11): chiusa e ripresa la partita in città, il portale c'è ancora.
    /// Sconfitto il boss, nella sua sala si apre il portale della vittoria (D9): porta in città e lì
    /// annuncia la vittoria.
    /// </summary>
    [DisallowMultipleComponent]
    public class TownPortals : MonoBehaviour, IPortalOpener
    {
        /// <summary>L'ingresso della città dove arriva chi attraversa un portale.</summary>
        public const string PortalEntrance = "Portal";

        [SerializeField] private LevelManager _levelManager;

        [SerializeField] private Portal _portalPrefab;

        [Tooltip("Le impostazioni dei livelli generati, cripta e caverne: dicono la scena di ogni profondità.")]
        [SerializeField] private DungeonSettings[] _dungeons;

        [SerializeField] private string _townScene = "Town";

        [Tooltip("Quanto davanti al cavaliere si apre il portale, in metri.")]
        [SerializeField, Min(1f)] private float _distance = 2.5f;

        [Tooltip("Quanto vicino al cavaliere può aprirsi, se davanti c'è un muro.")]
        [SerializeField, Min(0.5f)] private float _minDistance = 1.2f;

        [Tooltip("In città, quanto dietro il cavaliere appena arrivato sta il portale.")]
        [SerializeField, Min(0f)] private float _behindEntrance = 2f;

        [Tooltip("La camera: l'ovale del portale la guarda.")]
        [SerializeField] private Transform _viewer;

        [Tooltip("Una sorgente senza posizione, di Core: il suono del viaggio non si taglia scaricando il livello.")]
        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _travelClip;

        // davanti, poi a destra, a sinistra e dietro, finché uno non ha spazio
        private static readonly Vector3[] Directions = { Vector3.forward, Vector3.right, Vector3.left, Vector3.back };

        private Transform _player;
        private Health _playerHealth;
        private Portal _open;
        private Portal _victory;
        private bool _victoryPending;

        /// <summary>La profondità a cui riporta il portale della città; 0 se non c'è.</summary>
        public int PortalDepth { get; private set; }

        /// <summary>Dove riporta il portale della città, nella sua profondità; null: all'ingresso.</summary>
        public Vector3? PortalSpot { get; private set; }

        /// <summary>Il portale aperto nel livello corrente, o null.</summary>
        public Portal OpenPortal => _open;

        /// <summary>Il portale della vittoria nel livello corrente, o null.</summary>
        public Portal VictoryPortal => _victory;

        /// <summary>Il cavaliere è arrivato in città dal portale della vittoria: la partita è vinta (D9 della M10).</summary>
        public event System.Action VictoryReached;

        /// <summary>
        /// Il portale della vittoria, che non chiude il portale della pergamena: si apre morto il boss,
        /// e di nuovo ogni volta che si torna nella sua sala.
        /// </summary>
        public void OpenVictoryPortal(Vector3 position, bool animate)
        {
            if (_victory != null)
            {
                return;
            }

            if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            _victory = Spawn(position);
            _victory.Open(TextKeys.PortalVictory, null, _viewer, animate);
        }

        public void Bind(Transform player)
        {
            _player = player;
            _playerHealth = player.GetComponent<Health>();
        }

        /// <summary>
        /// Profondità e punto del portale della città, da un salvataggio; 0 per una partita nuova. Senza
        /// punto, come nei salvataggi di prima, il portale riporta all'ingresso della profondità.
        /// </summary>
        public void Restore(int portalDepth, Vector3? portalSpot = null)
        {
            PortalDepth = Mathf.Max(0, portalDepth);
            PortalSpot = PortalDepth > 0 ? portalSpot : null;
        }

        /// <summary>La scena generata che contiene <paramref name="depth"/>; null se nessuna.</summary>
        public string SceneFor(int depth)
        {
            foreach (var dungeon in _dungeons)
            {
                if (dungeon != null && dungeon.Covers(depth))
                {
                    return dungeon.SceneName;
                }
            }

            return null;
        }

        public bool TryOpenPortal()
        {
            var level = _levelManager.CurrentLevel;
            if (level == null || level.IsTown || _levelManager.IsTransitioning || _player == null || _playerHealth.IsDead)
            {
                return false;
            }

            // uno solo per volta: una pergamena nuova sposta il portale
            Close();
            _open = Spawn(FindSpot());
            _open.Open(TextKeys.PortalToTown, null, _viewer, animate: true);
            return true;
        }

        private Vector3 FindSpot()
        {
            Vector3 origin = _player.position;
            Quaternion facing = Quaternion.LookRotation(Flat(_player.forward));
            foreach (var direction in Directions)
            {
                Vector3 step = facing * direction;
                Vector3 target = origin + step * _distance;

                // fino al muro, se c'è, ma non addosso al cavaliere
                if (!NavMesh.Raycast(origin, target, out var hit, NavMesh.AllAreas))
                {
                    return target;
                }

                if (hit.distance >= _minDistance)
                {
                    return hit.position - step * 0.3f;
                }
            }

            return origin + facing * Vector3.forward * _minDistance;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        // nella scena attiva, quella del livello: si scarica con lui
        private Portal Spawn(Vector3 position)
        {
            var portal = Instantiate(_portalPrefab, position, Quaternion.identity);
            portal.Entered += HandleEntered;
            return portal;
        }

        private void Close()
        {
            if (_open != null)
            {
                _open.Entered -= HandleEntered;
                Destroy(_open.gameObject);
            }

            _open = null;
        }

        private void OnEnable()
        {
            _levelManager.LevelLoaded += HandleLevelLoaded;
            _levelManager.LevelUnloading += HandleLevelUnloading;
        }

        private void OnDisable()
        {
            _levelManager.LevelLoaded -= HandleLevelLoaded;
            _levelManager.LevelUnloading -= HandleLevelUnloading;
        }

        // in città, se c'è una profondità a cui tornare, il portale aspetta dietro il cavaliere; arrivati
        // dal portale della vittoria, la vittoria
        private void HandleLevelLoaded(LevelContext level)
        {
            if (_victoryPending && level.IsTown)
            {
                _victoryPending = false;
                VictoryReached?.Invoke();
            }

            if (!level.IsTown || PortalDepth <= 0 || !level.HasEntrance(PortalEntrance))
            {
                return;
            }

            Transform entrance = level.GetEntrance(PortalEntrance);
            _open = Spawn(entrance.position - entrance.forward * _behindEntrance);
            _open.Open(TextKeys.PortalToDepth, PortalDepth.ToString(), _viewer, animate: false);
        }

        // il portale sparisce con la sua scena: basta staccarsi
        private void HandleLevelUnloading(LevelContext level)
        {
            if (_open != null)
            {
                _open.Entered -= HandleEntered;
            }

            if (_victory != null)
            {
                _victory.Entered -= HandleEntered;
            }

            _open = null;
            _victory = null;
        }

        private void HandleEntered(Portal portal)
        {
            var level = _levelManager.CurrentLevel;
            if (level == null || _levelManager.IsTransitioning || _playerHealth.IsDead)
            {
                return;
            }

            if (_audio != null && _travelClip != null)
            {
                _audio.PlayOneShot(_travelClip);
            }

            if (portal == _victory)
            {
                _victoryPending = true;
                _levelManager.LoadLevel(_townScene, PortalEntrance, 0);
                return;
            }

            if (!level.IsTown)
            {
                PortalDepth = level.Depth;
                PortalSpot = portal.transform.position;
                _levelManager.LoadLevel(_townScene, PortalEntrance, 0);
                return;
            }

            // riattraversato, il portale si chiude
            int depth = PortalDepth;
            string scene = SceneFor(depth);
            if (scene == null)
            {
                Debug.LogError($"Nessuna scena generata per la profondità {depth}.", this);
                return;
            }

            Vector3? spot = PortalSpot;
            PortalDepth = 0;
            PortalSpot = null;
            if (spot.HasValue)
            {
                _levelManager.LoadLevelAt(scene, DungeonLevel.EntranceAt(depth), depth, spot.Value);
            }
            else
            {
                _levelManager.LoadLevel(scene, DungeonLevel.EntranceAt(depth), depth);
            }
        }
    }
}
