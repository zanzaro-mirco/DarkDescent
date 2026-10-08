using System;
using System.IO;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Progression;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Save
{
    /// <summary>
    /// Salva e ricarica la partita (D7, D8, D10 della M8). Salva entrando in un livello, ogni minuto
    /// fuori dal combattimento e chiudendo il gioco, in <c>persistentDataPath/save.json</c>. Nell'editor
    /// è spento, così le prove e i test non toccano il salvataggio vero: i test lo accendono con un
    /// file loro, da codice (<see cref="UseFile"/>) o, per provare la ripresa all'avvio, con la
    /// variabile d'ambiente <see cref="FileVariable"/>. Chi decide se caricare all'avvio è il
    /// composition root.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveGame : MonoBehaviour
    {
        /// <summary>Un file di salvataggio scelto da fuori: accende il salvataggio anche nell'editor.</summary>
        public const string FileVariable = "DARKDESCENT_SAVE_FILE";

        [SerializeField] private string _fileName = "save.json";

        [Tooltip("Ogni quanti secondi si salva, fuori dal combattimento.")]
        [SerializeField, Min(5f)] private float _interval = 60f;

        [Tooltip("Secondi senza colpi dati o presi perché il cavaliere sia fuori dal combattimento.")]
        [SerializeField, Min(0f)] private float _combatCooldown = 5f;

        [SerializeField] private ItemDatabase _items;

        [SerializeField] private AffixDatabase _affixes;

        private LevelManager _levelManager;
        private PlayerInventory _inventory;
        private PlayerProgress _progress;
        private CharacterStats _stats;
        private Health _health;
        private MeleeAttack _attack;
        private ExplorationTracker _exploration;
        private string _path;
        private bool _forced;
        private bool _subscribed;
        private float _timer;
        private float _lastCombat = float.NegativeInfinity;

        /// <summary>Un salvataggio è stato scritto: per i test.</summary>
        public event Action<SaveData> Saved;

        /// <summary>Acceso: nelle build sempre, nell'editor solo se un test ha scelto un file.</summary>
        public bool IsActive => _forced || !Application.isEditor || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(FileVariable));

        // calcolato al primo uso e non in Awake: il composition root legge il salvataggio dal suo
        // Awake, che può arrivare prima di quello di questo componente
        public string FilePath => _path ??= ExternalPath() ?? Path.Combine(Application.persistentDataPath, _fileName);

        public void Bind(LevelManager levelManager, GameObject player, ExplorationTracker exploration)
        {
            Unsubscribe();
            _levelManager = levelManager;
            _exploration = exploration;
            _inventory = player.GetComponent<PlayerInventory>();
            _progress = player.GetComponent<PlayerProgress>();
            _stats = player.GetComponent<CharacterStats>();
            _health = player.GetComponent<Health>();
            _attack = player.GetComponent<MeleeAttack>();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private static string ExternalPath()
        {
            string path = Environment.GetEnvironmentVariable(FileVariable);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        /// <summary>Per i test: salva e legge da questo file, anche nell'editor.</summary>
        public void UseFile(string path)
        {
            _path = path;
            _forced = true;
        }

        public SaveReadResult TryLoad(out SaveData data)
        {
            data = null;
            return IsActive ? SaveFile.TryRead(FilePath, out data) : SaveReadResult.Missing;
        }

        /// <summary>Lo stato di adesso. Da morto si salva come dopo Continua: vivo, a vita piena (ADR-048).</summary>
        public SaveData Capture()
        {
            var sheet = _stats.Sheet;
            var progress = _progress.Progress;
            float life = _health.IsDead ? _health.Max : _health.Current;
            return SaveData.Create(_levelManager.RunSeed, _levelManager.CurrentSceneName, _levelManager.CurrentEntrance, _levelManager.CurrentLevel.Depth)
                .WithProgress(progress.Level, progress.Experience, progress.UnspentPoints)
                .WithAttributes(sheet.GetBase(StatType.Strength), sheet.GetBase(StatType.Dexterity), sheet.GetBase(StatType.Magic), sheet.GetBase(StatType.Vitality))
                .WithLife(life)
                .WithInventory(InventorySnapshot.Capture(_inventory.Inventory))
                .WithExplored(_exploration.Memory.Export());
        }

        /// <summary>Scrive lo stato di adesso; false se spento o se non c'è un livello in cui essere.</summary>
        public bool Save()
        {
            if (!IsActive || _levelManager == null || _levelManager.CurrentLevel == null || _levelManager.IsTransitioning)
            {
                return false;
            }

            try
            {
                var data = Capture();
                SaveFile.Write(FilePath, data);
                _timer = 0f;
                Saved?.Invoke(data);
                return true;
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[DarkDescent] salvataggio non riuscito: {exception.Message}");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning($"[DarkDescent] salvataggio non riuscito: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Rimette sul cavaliere quello che dice il salvataggio. Il livello lo carica il gestore dei
        /// livelli, dall'ingresso salvato. Va chiamato dopo gli Awake e gli OnEnable di tutti, quando
        /// vita e pannelli ascoltano già la scheda: l'inventario si svuota prima di ripristinarlo, quindi
        /// arma e pozioni di partenza, se già date, spariscono.
        /// </summary>
        public bool Apply(SaveData data)
        {
            var sheet = _stats.Sheet;
            sheet.SetBase(StatType.Strength, data.Strength);
            sheet.SetBase(StatType.Dexterity, data.Dexterity);
            sheet.SetBase(StatType.Magic, data.Magic);
            sheet.SetBase(StatType.Vitality, data.Vitality);
            _progress.Progress.Restore(data.Level, data.Experience, data.UnspentPoints);
            bool complete = data.Inventory.Restore(_inventory.Inventory, _items, _affixes);
            _health.SetCurrent(data.Life);

            // le mappe si riprendono quando il livello di ogni profondità viene generato
            _exploration.Memory.Import(data.Explored);
            return complete;
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
            if (_subscribed || _levelManager == null)
            {
                return;
            }

            _levelManager.LevelLoaded += HandleLevelLoaded;
            _health.Damaged += HandleDamaged;
            _attack.HitLanded += HandleHitLanded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _levelManager.LevelLoaded -= HandleLevelLoaded;
            _health.Damaged -= HandleDamaged;
            _attack.HitLanded -= HandleHitLanded;
            _subscribed = false;
        }

        private void HandleLevelLoaded(LevelContext level)
        {
            Save();
        }

        private void HandleDamaged(DamageInfo info, float applied)
        {
            _lastCombat = Time.unscaledTime;
        }

        private void HandleHitLanded(DamageInfo info)
        {
            _lastCombat = Time.unscaledTime;
        }

        // un conto alla rovescia, non un controllo dello stato: in combattimento si aspetta
        private void Update()
        {
            if (!IsActive || _health == null)
            {
                return;
            }

            _timer += Time.unscaledDeltaTime;
            if (_timer >= _interval && Time.unscaledTime - _lastCombat >= _combatCooldown && !_health.IsDead)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
