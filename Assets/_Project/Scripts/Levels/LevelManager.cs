using System;
using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Player;
using DarkDescent.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Carica i livelli sopra Core in modo additivo, uno alla volta, e ci porta il player (D1 della
    /// scheda M3). Player, camera e HUD stanno in Core e sopravvivono al cambio senza DontDestroyOnLoad.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("Scena caricata all'avvio di Core, se nessun livello è già aperto, alla profondità 1. Dalla M6 è la cripta generata, e Ricomincia ricarica il livello in cui si è morti (D13).")]
        [SerializeField] private string _firstLevel = "Level_Crypt";

        [SerializeField] private string _firstEntrance = "Start";

        [SerializeField] private PlayerController _player;

        [SerializeField] private ScreenFader _fader;

        private NavMeshAgent _playerAgent;
        private MeleeAttack _playerAttack;
        private Health _playerHealth;
        private Scene _currentScene;
        private string _currentEntrance;

        /// <summary>Livello pronto, player già sull'ingresso: è il momento di collegare i nemici.</summary>
        public event Action<LevelContext> LevelLoaded;

        /// <summary>Il livello sta per essere scaricato: è l'ultimo momento per staccarsi dai suoi oggetti.</summary>
        public event Action<LevelContext> LevelUnloading;

        public LevelContext CurrentLevel { get; private set; }

        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Il seme della partita, da cui ogni livello generato ricava il suo (D8 della M6). Lo imposta
        /// il composition root prima del primo caricamento; i test lo cambiano per rigiocare un dungeon.
        /// </summary>
        public ulong RunSeed { get; set; }

        private void Awake()
        {
            _playerAgent = _player.GetComponent<NavMeshAgent>();
            _playerAttack = _player.GetComponent<MeleeAttack>();
            _playerHealth = _player.GetComponent<Health>();
        }

        private void Start()
        {
            // Nell'editor si può premere Play con Core e un livello aperti insieme, e i test caricano
            // Core e la sandbox nello stesso frame: si usa il livello già caricato invece di caricarne
            // una seconda copia (trappola 6).
            if (TryFindLoadedLevel(out LevelContext loaded))
            {
                Enter(loaded, _firstEntrance);
                return;
            }

            LoadLevel(_firstLevel, _firstEntrance, 1);
        }

        private void OnDisable()
        {
            if (CurrentLevel != null)
            {
                CurrentLevel.ExitRequested -= HandleExitRequested;
            }
        }

        /// <param name="depth">La profondità del livello: conta per i livelli generati, quelli fatti a mano hanno la loro.</param>
        public void LoadLevel(string sceneName, string entranceId, int depth)
        {
            if (IsTransitioning)
            {
                return;
            }

            StartCoroutine(Transition(sceneName, entranceId, depth, null));
        }

        /// <summary>
        /// Ricarica il livello corrente dallo stesso ingresso e alla stessa profondità: con lo stesso
        /// seme della partita è la stessa cripta, con nemici e casse rimessi (D13 della M6).
        /// <paramref name="whileDark"/> parte a schermo nero, con il livello vecchio già scaricato e
        /// il nuovo non ancora caricato: lì si rimette a posto il player, senza nemici attorno.
        /// </summary>
        public void RestartLevel(Action whileDark)
        {
            if (IsTransitioning || CurrentLevel == null)
            {
                return;
            }

            StartCoroutine(Transition(_currentScene.name, _currentEntrance, CurrentLevel.Depth, whileDark));
        }

        private void HandleExitRequested(LevelExit exit)
        {
            // un morto che scivola sulle scale non cambia livello
            if (!_playerHealth.IsDead)
            {
                LoadLevel(exit.TargetScene, exit.TargetEntrance, exit.TargetDepth > 0 ? exit.TargetDepth : CurrentLevel.Depth + 1);
            }
        }

        private IEnumerator Transition(string sceneName, string entranceId, int depth, Action whileDark)
        {
            IsTransitioning = true;

            // niente click durante il cambio, e nessun riferimento a oggetti che stanno per sparire
            // (trappola 5): il bersaglio del player è nel livello vecchio
            _player.enabled = false;
            _playerAttack.ClearTarget();

            if (CurrentLevel != null)
            {
                // il player si ferma sul posto mentre lo schermo diventa nero
                if (_playerAgent.isOnNavMesh)
                {
                    _playerAgent.ResetPath();
                }

                yield return _fader.FadeTo(1f);
            }

            // un agent sopra un NavMesh che viene scaricato resta senza appoggio: lo si spegne prima
            _playerAgent.enabled = false;

            if (CurrentLevel != null)
            {
                CurrentLevel.ExitRequested -= HandleExitRequested;
                LevelUnloading?.Invoke(CurrentLevel);
                CurrentLevel = null;
                yield return SceneManager.UnloadSceneAsync(_currentScene);
            }

            whileDark?.Invoke();

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            // un livello generato si costruisce qui, a schermo nero (trappola 6)
            if (!TryGetContext(SceneManager.GetSceneByName(sceneName), depth, out LevelContext context))
            {
                Debug.LogError($"La scena {sceneName} non ha un LevelContext alla radice.", this);
                IsTransitioning = false;
                yield break;
            }

            Enter(context, entranceId);
        }

        private void Enter(LevelContext context, string entranceId)
        {
            _currentScene = context.gameObject.scene;
            _currentEntrance = entranceId;

            // luci, ambiente e nebbia vengono dalla scena attiva, e lì finiscono gli Instantiate (trappola 1)
            SceneManager.SetActiveScene(_currentScene);
            CurrentLevel = context;
            context.ExitRequested += HandleExitRequested;

            // Spento, spostato, riacceso: riaccendendosi l'agent si aggancia al NavMesh del livello. Con
            // l'agent acceso si userebbe Warp, ma qui il NavMesh sotto il player è appena cambiato.
            Transform entrance = context.GetEntrance(entranceId);
            _playerAgent.enabled = false;
            _player.transform.SetPositionAndRotation(entrance.position, entrance.rotation);

            if (!_playerHealth.IsDead)
            {
                _playerAgent.enabled = true;
                _player.enabled = true;
            }

            IsTransitioning = false;
            LevelLoaded?.Invoke(context);
            StartCoroutine(_fader.FadeTo(0f));
        }

        private bool TryFindLoadedLevel(out LevelContext context)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != gameObject.scene && scene.isLoaded && TryGetContext(scene, 1, out context))
                {
                    return true;
                }
            }

            context = null;
            return false;
        }

        // Il contesto alla radice della scena; in una scena di livello generato, il livello lo costruisce
        // il suo DungeonLevel, alla profondità chiesta.
        private bool TryGetContext(Scene scene, int depth, out LevelContext context)
        {
            context = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out context))
                {
                    return true;
                }
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out DungeonLevel dungeon))
                {
                    context = dungeon.Build(RunSeed, depth);
                    return true;
                }
            }

            return false;
        }
    }
}
