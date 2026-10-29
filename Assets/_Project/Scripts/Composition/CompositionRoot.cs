using System;
using System.Collections.Generic;
using System.Globalization;
using DarkDescent.Audio;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Save;
using DarkDescent.Stats;
using DarkDescent.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace DarkDescent.Core
{
    /// <summary>
    /// Collega i sistemi in un posto solo, leggibile dall'alto in basso (ADR-007). Sta in Core: collega
    /// HUD e hit stop al player una volta, e i nemici di ogni livello quando il LevelManager lo carica.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompositionRoot : MonoBehaviour
    {
        [SerializeField] private Health _player;

        private readonly RunStats _runStats = new RunStats();

        // presi in Awake: allo scarico della scena il cavaliere può essere distrutto prima di OnDisable
        private Inventory _playerInventory;

        [SerializeField] private LevelManager _levelManager;

        [SerializeField] private HealthOrb _healthOrb;

        [SerializeField] private DeathScreen _deathScreen;

        [SerializeField] private DamageNumbers _damageNumbers;

        [SerializeField] private InteractableLabel _interactableLabel;

        [SerializeField] private InventoryPanel _inventoryPanel;

        [SerializeField] private CharacterPanel _characterPanel;

        [Tooltip("La finestra del mercante, a sinistra (D5 della M10).")]
        [SerializeField] private ShopPanel _shopPanel;

        [Tooltip("Cosa vende il mercante: provviste e basi degli oggetti (D5 della M10).")]
        [SerializeField] private MerchantTable _merchantTable;

        [SerializeField] private BeltView _beltView;

        [SerializeField] private ExplorationTracker _exploration;

        [SerializeField] private AutomapView _automap;

        [SerializeField] private ItemCursor _itemCursor;

        [SerializeField] private HitStop _hitStop;

        [SerializeField] private CinemachineCamera _playerCamera;

        [Tooltip("Musica, fondo e versi nel buio: cambiano con il livello (passo 7.0 della M7).")]
        [SerializeField] private AmbiencePlayer _ambience;

        [Tooltip("La musica dello scontro con il boss (D5 della M11): entra con la porta che si chiude, sfuma alla vittoria.")]
        [SerializeField] private AmbienceProfile _bossAmbience;

        [Tooltip("Il limite di voci dei personaggi (D9 della M7): cavaliere e nemici gli chiedono il permesso.")]
        [SerializeField] private SfxLimiter _sfxLimiter;

        [Tooltip("La scritta del livello in cui si è: grande entrando, piccola sotto la minimappa.")]
        [SerializeField] private LevelTitle _levelTitle;

        [Tooltip("Gli avvisi al centro dello schermo: zaino pieno, portale che non si apre.")]
        [FormerlySerializedAs("_inventoryFullMessage")]
        [SerializeField] private HudMessage _hudMessage;

        [Tooltip("L'icona del bonus del santuario accanto alla cintura (D6 della M13).")]
        [SerializeField] private ShrineBonusView _shrineBonusView;

        [SerializeField] private ExperienceBar _experienceBar;

        [SerializeField] private PointsButton _pointsButton;

        [SerializeField] private GroundLabels _groundLabels;

        [SerializeField] private MainMenu _mainMenu;

        [SerializeField] private PauseMenu _pauseMenu;

        [SerializeField] private OptionsPanel _optionsPanel;

        [Tooltip("La sfera blu del mana, in basso a destra (M9).")]
        [SerializeField] private ManaOrb _manaOrb;

        [Tooltip("I tre incantesimi accanto alla cintura (M9).")]
        [SerializeField] private SpellBarView _spellBar;

        [Tooltip("La magia del portale, a sinistra della cintura (passo 11.7 della M11).")]
        [SerializeField] private PortalSlotView _portalSlot;

        [Tooltip("I proiettili di tutti, in un pool per prefab (M9).")]
        [SerializeField] private Magic.ProjectilePools _projectiles;

        [SerializeField] private SaveGame _saveGame;

        [Tooltip("Nome e vita del nemico sotto il cursore, o di quello combattuto (M9), in alto al centro.")]
        [SerializeField] private EnemyBar _enemyBar;

        [Tooltip("I numeri dei nemici speciali, campioni e unici, e i loro modificatori (M12).")]
        [SerializeField] private ChampionSettings _championSettings;

        [Tooltip("Il cerchio rosso a terra sotto il nemico che ha il cursore addosso.")]
        [SerializeField] private Rendering.TargetMarker _targetMarker;

        [Tooltip("La tabella delle stringhe: una colonna per lingua (D12 della M5).")]
        [SerializeField] private TextAsset _strings;

        [Tooltip("Gli affissi che il generatore può tirare (D3 della M5).")]
        [SerializeField] private AffixDatabase _affixes;

        [Tooltip("Gli unici e i pezzi di set che il loot può far cadere (M16).")]
        [SerializeField] private NamedItemDatabase _namedItems;

        [Tooltip("Le pergamene e quanto spesso un livello ne ha una (D5 della M9).")]
        [SerializeField] private ScrollTable _scrolls;

        [Tooltip("I portali per l'accampamento, che apre la magia del portale (D6 della M10, passo 11.7 della M11).")]
        [SerializeField] private Town.TownPortals _portals;

        [Tooltip("Il filtro retrò, regolato dalle opzioni (D2 della M11).")]
        [SerializeField] private Rendering.RetroFilter _retroFilter;

        [Tooltip("Ricorda i livelli lasciati e li rimette com'erano (10.9 della M10).")]
        [SerializeField] private LevelRecorder _levelRecorder;

        [Tooltip("La vittoria e i riconoscimenti (D9 della M10).")]
        [SerializeField] private VictoryScreen _victoryScreen;

        [Tooltip("Le difficoltà, dal Crepuscolo all'ultimo Abisso (D2 della M14).")]
        [SerializeField] private DifficultyTable _difficulties;

        [Tooltip("Il pannello dell'altare dell'accampamento (D3 della M14).")]
        [SerializeField] private DifficultyPanel _difficultyPanel;

        [Tooltip("La scena della città, dove comincia ogni partita nuova (D3 della M10).")]
        [SerializeField] private string _townScene = "Town";

        /// <summary>Dove resta la lingua scelta tra un avvio e l'altro: è una preferenza, non la partita.</summary>
        public const string LanguagePreference = "language";

        private const string LanguageOption = "-lang";

        private const string SeedOption = "-seed";

        // una partita nuova a una difficoltà e a un livello del cavaliere, per provarle (D13 della M14)
        private const string DifficultyOption = "-difficulty";
        private const string HeroLevelOption = "-herolevel";
        private const string NamedChanceOption = "-namedchance";

        /// <summary>Opzioni in più, separate da spazi, come se fossero sulla riga di comando: per i test.</summary>
        public const string ArgumentsVariable = "DARKDESCENT_ARGS";

        // salta il menu e comincia una partita nuova, senza salvarla (D2 della M10)
        private const string NewGameOption = "-newgame";

        /// <summary>L'ingresso della città da cui comincia una partita nuova.</summary>
        public const string TownEntrance = "Start";

        /// <summary>"1" mostra il menu iniziale anche nell'editor: per i test del menu.</summary>
        public const string MenuVariable = "DARKDESCENT_MENU";

        /// <summary>La cartella dei tre salvataggi invece di quella del gioco: per i test.</summary>
        public const string SaveFolderVariable = "DARKDESCENT_SAVE_DIR";

        // i tre posti del menu iniziale; null quando il gioco parte senza menu
        private SaveSlots _saves;

        private MeleeAttack _playerAttack;
        private MovementSlow _playerSlow;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private LootRoller _loot;
        private readonly List<SpecialEnemy> _specialsOnMap = new List<SpecialEnemy>();
        private Footsteps _footsteps;
        private PlayerProgress _progress;
        private Rendering.CameraRotation _cameraRotation;
        private Magic.PlayerMagic _magic;
        private PlayerInventory _inventory;

        // i banchi del borgo, mercante, fabbro e guaritrice, se è il livello corrente (D5 della M10,
        // D18 della M18): un banco per ognuno, con la sua merce
        private readonly List<Town.Merchant> _merchants = new List<Town.Merchant>();
        private readonly Dictionary<Town.Merchant, Shop> _shops = new Dictionary<Town.Merchant, Shop>();
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();

        // l'altare della città, se è il livello corrente (D3 della M14)
        private Town.DifficultyAltar _altar;

        // il re è caduto per la prima volta a questa difficoltà: arrivati in città, la vittoria (D15 della M14)
        private bool _firstVictory;

        // il boss del livello corrente, se c'è e non è ancora stato sconfitto (D8 della M10)
        private BossController _boss;

        // quali pergamene possono ancora cadere: creata una volta, non a ogni livello
        private Func<Magic.SpellDefinition, bool> _scrollWanted;

        // muri e torce del livello corrente, alti o bassi secondo la camera (D12 della M8); null nei livelli fatti a mano senza
        private WallView _walls;
        private readonly List<EnemyAI> _rewardingEnemies = new List<EnemyAI>();
        private int _depth;

        // il salvataggio da rimettere sul cavaliere in Start, quando tutti ascoltano già (D10 della M8)
        private SaveData _pendingSave;

        // a che difficoltà si gioca (D3 della M14), e il livello di -herolevel da dare in Start (0: niente)
        private DifficultyProgress _difficulty;
        private int _heroLevel;
        private readonly List<string> _bossModifiers = new List<string>();

        // un generatore solo per tutti i tiri del combattimento, con un seme diverso a ogni avvio (D3)
        private IRandomSource _random;

        private readonly List<MeleeAttack> _enemyAttacks = new List<MeleeAttack>();
        private readonly List<RangedAttack> _enemyRanged = new List<RangedAttack>();

        // i branchi del livello corrente: chi vede il cavaliere sveglia i compagni (D5 della M7)
        private EnemyPack _pack;

        // le Health dei nemici seguite dai numeri di danno: al cambio di livello vanno lasciate, anche
        // quelle già distrutte, di cui non si potrebbe più chiedere il componente all'EnemyAI
        private readonly List<Health> _trackedEnemies = new List<Health>();

        /// <summary>La lingua del gioco: i test la leggono e la cambiano da qui.</summary>
        public Localizer Localizer => _localizer;

        /// <summary>Il seme della partita, da cui viene ogni drop (D5 della M5).</summary>
        public ulong LootSeed => _loot.RunSeed;

        /// <summary>La difficoltà della partita, quelle completate e il mondo (M14).</summary>
        public DifficultyProgress Difficulty => _difficulty;

        /// <summary>Il gradino della difficoltà attuale.</summary>
        public DifficultyTier CurrentDifficulty => _difficulties.Tier(_difficulty.Current);

        private void Awake()
        {
            // la lingua per prima: chi viene collegato dopo mostra già il testo giusto. La riga di
            // comando vince sulla preferenza salvata, che vince sull'inglese
            _localizer = new Localizer(StringTable.Parse(_strings.text));
            if (!CommandLine.TryGetValue(Environment.GetCommandLineArgs(), LanguageOption, out string language))
            {
                language = PlayerPrefs.GetString(LanguagePreference, Localizer.DefaultLanguage);
            }

            _localizer.SetLanguage(language);
            foreach (var text in FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                text.Bind(_localizer);
            }

            // il seme del loot: da riga di comando per rigiocare una partita, altrimenti dall'orologio.
            // Separato dai tiri del combattimento: un colpo mancato in più non cambia i drop (D5)
            string[] args = Arguments();
            _difficulty = new DifficultyProgress(_difficulties.Count);
            bool seedGiven = CommandLine.TryGetValue(args, SeedOption, out string seedText) && ulong.TryParse(seedText, out _);
            if (!seedGiven || !ulong.TryParse(seedText, out ulong seed))
            {
                seed = (ulong)DateTime.UtcNow.Ticks;
            }

            // un salvataggio, se c'è, decide seme e livello (D10 della M8); -newgame e -seed lo ignorano
            _portals.Bind(_player.transform);
            _saveGame.Bind(_levelManager, _player.gameObject, _exploration, _runStats, _portals, _levelRecorder, _difficulty);
            // il menu iniziale nelle build, e nell'editor solo se un test lo chiede (D2 della M10).
            // -newgame e -seed lo saltano e giocano senza salvare; un file scelto da fuori si riprende
            bool newGame = CommandLine.HasFlag(args, NewGameOption);
            bool fileGiven = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SaveGame.FileVariable));
            bool menuWanted = !Application.isEditor || Environment.GetEnvironmentVariable(MenuVariable) == "1";
            if (!seedGiven && !newGame && !fileGiven && menuWanted)
            {
                _saves = new SaveSlots(SaveFolder());
                if (_saves.MigrateLegacy())
                {
                    Debug.Log("[DarkDescent] il salvataggio di prima è ora il posto 1");
                }

                _levelManager.HoldStart();
            }
            else if (seedGiven || newGame)
            {
                StartInTown();
                ReadNewGameOptions(args);
            }
            else
            {
                SaveReadResult read = _saveGame.TryLoad(out SaveData save);
                if (read == SaveReadResult.Ok)
                {
                    seed = save.RunSeed;
                    _levelManager.SetStartLevel(save.Scene, save.Entrance, save.Depth);
                    _pendingSave = save;
                    Debug.Log($"[DarkDescent] partita ripresa dal salvataggio: {save.Scene}, profondità {save.Depth}, livello {save.Level}");
                }
                else if (read != SaveReadResult.Missing)
                {
                    Debug.LogWarning($"[DarkDescent] salvataggio non caricato ({read}): partita nuova, e il prossimo salvataggio lo sostituisce");
                }

                if (read != SaveReadResult.Ok)
                {
                    ReadNewGameOptions(args);
                }
            }

            // il livello degli oggetti è quello della zona, con la difficoltà (D7 della M14)
            var progression = _player.GetComponent<PlayerProgress>().Settings;
            _loot = new LootRoller(new ItemGenerator(_affixes.Affixes, RarityTable.Default), seed,
                depth => progression.ZoneLevel(depth, CurrentDifficulty.ZoneLevelBonus));
            _loot.UseNamed(_namedItems);
            ReadNamedChance(args);
            ApplyDifficultyToLoot();
            // lo stesso seme fa anche i livelli generati: ognuno ne ricava uno suo (D8 della M6)
            _levelManager.RunSeed = seed;
            Debug.Log($"[DarkDescent] seme della partita: {seed} (per rigiocarla: -seed {seed})");

            _playerAttack = _player.GetComponent<MeleeAttack>();
            _playerSlow = _player.GetComponent<MovementSlow>();
            _playerInventory = _player.GetComponent<PlayerInventory>().Inventory;
            _random = new SystemRandomSource(Environment.TickCount);
            _playerAttack.SetRandomSource(_random);

            _healthOrb.Bind(_player);
            _deathScreen.Bind(_player);
            _damageNumbers.SetLocalizer(_localizer);
            _damageNumbers.Track(_player, isPlayer: true);
            _interactableLabel.Bind(_player.GetComponent<PlayerController>(), _localizer);

            var inventory = _player.GetComponent<PlayerInventory>();
            _reader = _player.GetComponent<PlayerInputReader>();
            _inventoryPanel.Bind(inventory, _reader, _localizer);
            _itemCursor.Bind(inventory, _reader);
            _progress = _player.GetComponent<PlayerProgress>();
            _characterPanel.Bind(_player.GetComponent<Stats.CharacterStats>(), inventory, _progress.Progress, _reader, _localizer);
            _experienceBar.Bind(_progress.Progress, _localizer);
            _pointsButton.Bind(_progress.Progress, _characterPanel);
            _shopPanel.Bind(null, inventory, _inventoryPanel, _localizer);
            _groundLabels.Bind(_reader, _player.GetComponent<PlayerController>(), _localizer);
            _optionsPanel.Bind(_localizer, _groundLabels, _retroFilter, new[] { _healthOrb.Readout, _manaOrb.Readout });
            _beltView.Bind(inventory);
            _exploration.Bind(_player.transform);
            _cameraRotation = _playerCamera.GetComponent<Rendering.CameraRotation>();
            _cameraRotation.Bind(_reader);
            _automap.Bind(_exploration, _reader, _cameraRotation);
            // un generatore suo: i versi nel buio non spostano i tiri del combattimento
            _ambience.Bind(_player.transform, new SystemRandomSource(Environment.TickCount ^ 0x5EED));
            _player.GetComponent<CharacterAudio>().Bind(_sfxLimiter);
            _footsteps = _player.GetComponentInChildren<Footsteps>();
            _footsteps.Bind(_sfxLimiter);
            _levelTitle.Bind(_localizer);
            _victoryScreen.Bind(_localizer);
            _difficultyPanel.Bind(_difficulties, _difficulty, _localizer);
            var controller = _player.GetComponent<PlayerController>();
            _enemyBar.Bind(controller, _localizer);
            _targetMarker.Bind(controller);
            var magic = _player.GetComponent<Magic.PlayerMagic>();
            _magic = magic;
            _inventory = inventory;
            _scrollWanted = spell => !_magic.Book.Knows(spell) && !_inventory.CarriesScrollOf(spell);
            magic.Bind(_reader, controller);
            magic.SetPortalOpener(_portals);
            var shrineBonus = _player.GetComponent<ShrineBonus>();
            _hudMessage.Bind(inventory, magic, _localizer, shrineBonus);
            _shrineBonusView.Bind(shrineBonus);
            _manaOrb.Bind(magic.Mana);
            _spellBar.Bind(magic);
            _portalSlot.Bind(magic);
            // un generatore suo: i danni degli incantesimi non spostano i tiri della mischia (trappola 3 della M9)
            _player.GetComponent<Magic.PlayerSpells>().Bind(_projectiles, new SystemRandomSource(Environment.TickCount ^ 0x5A11));
            _playerCamera.GetComponent<Rendering.CameraZoom>().Bind(_reader);
        }

        private void Start()
        {
            if (_pendingSave != null)
            {
                _saveGame.Apply(_pendingSave);
                _pendingSave = null;
            }

            if (_heroLevel > 0)
            {
                PrepareHero(_heroLevel);
            }

            if (_saves != null)
            {
                // fermo finché non si sceglie: niente tasti del gioco sotto il menu
                _reader.enabled = false;
                _mainMenu.Show(_saves, _localizer, _difficulties);
            }
        }

        // Una partita nuova comincia in città (D3 della M10). Senza menu e senza riga di comando, cioè
        // nell'editor e nei test, si parte invece dalla cripta, come prima: i test cercano i nemici lì.
        private void StartInTown()
        {
            _levelManager.SetStartLevel(_townScene, TownEntrance, 0);
            _portals.Restore(0);
        }

        // la riga di comando e, dietro, le opzioni della variabile: quella dell'editor non si cambia
        private static string[] Arguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            string extra = Environment.GetEnvironmentVariable(ArgumentsVariable);
            if (string.IsNullOrWhiteSpace(extra))
            {
                return args;
            }

            string[] more = extra.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var all = new string[args.Length + more.Length];
            args.CopyTo(all, 0);
            more.CopyTo(all, args.Length);
            return all;
        }

        // -difficulty e -herolevel (D13 della M14): solo in una partita nuova, non in una ripresa.
        // La difficoltà chiesta è aperta, come se si fossero battuti i re prima di lei
        private void ReadNewGameOptions(string[] args)
        {
            if (CommandLine.TryGetValue(args, DifficultyOption, out string text) && int.TryParse(text, out int difficulty))
            {
                difficulty = Mathf.Clamp(difficulty, 0, _difficulties.Count - 1);
                _difficulty.Restore(difficulty, difficulty - 1, 0);
                Debug.Log($"[DarkDescent] difficoltà {difficulty} da riga di comando");
            }

            if (CommandLine.TryGetValue(args, HeroLevelOption, out text) && int.TryParse(text, out int level))
            {
                _heroLevel = Math.Max(0, level);
            }
        }

        /// <summary>
        /// Il cavaliere di <c>-herolevel</c>: al livello dato, con i punti spesi e un oggetto raro per
        /// slot di quel livello, dal seme della partita, e a vita piena. Anche per i test.
        /// </summary>
        public void PrepareHero(int level)
        {
            HeroSetup.RaiseTo(_progress.Progress, level);
            int equipped = HeroSetup.Equip(_playerInventory.Equipment, _merchantTable.Stock, _loot.Generator, level, _loot.RunSeed);
            _player.Heal(_player.Max);
            Debug.Log($"[DarkDescent] cavaliere di livello {level} da riga di comando, {equipped} oggetti");
        }

        // rarità e oro del loot seguono la difficoltà (D5 e D11 della M14); il livello degli oggetti la legge da sé
        private void ApplyDifficultyToLoot()
        {
            var tier = CurrentDifficulty;
            _loot.UseDifficulty(tier.Rarities, tier.Gold);
            _loot.UseNamedChances(tier.UniqueChance, tier.SetChance);
        }

        // -namedchance (D15 della M16): unici e set più spessi, per provarli. Vale anche in una ripresa
        private void ReadNamedChance(string[] args)
        {
            if (CommandLine.TryGetValue(args, NamedChanceOption, out string text)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float factor) && factor > 0f)
            {
                _loot.NamedChanceFactor = factor;
                Debug.Log($"[DarkDescent] probabilità di unici e set ×{factor} da riga di comando");
            }
        }

        private static string SaveFolder()
        {
            string folder = Environment.GetEnvironmentVariable(SaveFolderVariable);
            return string.IsNullOrEmpty(folder) ? Application.persistentDataPath : folder;
        }

        /// <summary>
        /// Una partita scelta nel menu: nel suo posto si salva; ripresa, decide seme e livello e torna
        /// sul cavaliere prima che il livello si colleghi (trappola 8 della M9); nuova, parte da capo.
        /// </summary>
        private void HandleMenuChosen(int slot, bool isNew)
        {
            _saveGame.UseFile(_saves.PathOf(slot));
            ulong seed = (ulong)DateTime.UtcNow.Ticks;
            _runStats.Restore(0f, 0, 0, 0, false);
            _difficulty.Restore(0, -1, 0);
            SaveData save = null;
            bool resumed = !isNew && _saves.TryRead(slot, out save);
            if (resumed)
            {
                seed = save.RunSeed;
                _levelManager.SetStartLevel(save.Scene, save.Entrance, save.Depth);
                _saveGame.Apply(save);
                Debug.Log($"[DarkDescent] partita ripresa dal posto {slot}: {save.Scene}, profondità {save.Depth}, livello {save.Level}");
            }
            else
            {
                StartInTown();
                Debug.Log($"[DarkDescent] partita nuova nel posto {slot}");
            }

            _loot.RunSeed = seed;
            _levelManager.RunSeed = seed;
            Debug.Log($"[DarkDescent] seme della partita: {seed} (per rigiocarla: -seed {seed})");

            // nelle build il menu c'è sempre: -difficulty e -herolevel valgono anche per una partita
            // nuova scelta qui (passo 14.7), dopo il seme, da cui viene l'equipaggiamento
            if (!resumed)
            {
                ReadNewGameOptions(Arguments());
                if (_heroLevel > 0)
                {
                    PrepareHero(_heroLevel);
                }
            }

            _mainMenu.Hide();
            _reader.enabled = true;
            _levelManager.Begin();
        }

        // Esc: prima chiude le finestre aperte, poi apre e chiude la pausa (D10 della M10)
        private void HandlePausePressed()
        {
            if (_mainMenu.IsShown)
            {
                return;
            }

            if (_pauseMenu.IsOpen)
            {
                _pauseMenu.Close();
                return;
            }

            if (_difficultyPanel.IsOpen)
            {
                _difficultyPanel.Close();
                return;
            }

            if (_inventoryPanel.IsOpen || _characterPanel.IsOpen)
            {
                _inventoryPanel.SetOpen(false);
                if (_characterPanel.IsOpen)
                {
                    _characterPanel.Toggle();
                }

                return;
            }

            if (_levelManager.CurrentLevel != null && !_levelManager.IsTransitioning && !_player.IsDead)
            {
                _pauseMenu.Open();
            }
        }

        // in pausa il cavaliere non ascolta i click e non lancia: alla ripresa non partirebbe da solo
        private void HandlePausedChanged(bool paused)
        {
            _player.GetComponent<PlayerController>().enabled = !paused;
            _magic.enabled = !paused;
        }

        // salva e ricarica la scena: nelle build riparte dal menu iniziale
        private void HandleQuitToMenu()
        {
            _saveGame.Save();
            _pauseMenu.Close();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        /// <summary>I contatori della partita (D11 della M10): tempo, uccisioni, morti, oro raccolto.</summary>
        public RunStats RunStats => _runStats;

        private void Update()
        {
            // il tempo di gioco: non quello passato nel menu o tra un livello e l'altro
            if (_levelManager.CurrentLevel != null)
            {
                _runStats.AddTime(Time.deltaTime);
            }
        }

        private void HandlePlayerDied()
        {
            _runStats.AddDeath();

            // morire costa (D9 della M14): una parte dell'oro e dell'esperienza del livello, mai un livello
            var tier = CurrentDifficulty;
            var progress = _progress.Progress;
            int gold = DeathPenalty.GoldLost(_playerInventory.Gold, tier.DeathGold);
            _playerInventory.TrySpendGold(gold);
            int experience = progress.Remove(DeathPenalty.ExperienceLost(progress.Experience, progress.ExperienceToNext, tier.DeathExperience));
            _deathScreen.ShowLoss(gold > 0 || experience > 0 ? _localizer.Format(TextKeys.DeathLoss, gold, experience) : null);
        }

        private void HandleGoldAdded(int amount)
        {
            _runStats.AddGold(amount);
        }

        private void OnEnable()
        {
            _player.Died += HandlePlayerDied;
            _mainMenu.Chosen += HandleMenuChosen;
            _reader.PausePressed += HandlePausePressed;
            _pauseMenu.PausedChanged += HandlePausedChanged;
            _pauseMenu.QuitRequested += HandleQuitToMenu;
            _optionsPanel.LanguageCycleRequested += CycleLanguage;
            _playerInventory.GoldAdded += HandleGoldAdded;
            _difficulty.Changed += ApplyDifficultyToLoot;
            _difficulty.WorldChanged += HandleWorldChanged;
            _difficultyPanel.SelectRequested += HandleDifficultySelected;
            _difficultyPanel.NewWorldRequested += HandleNewWorldRequested;
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
            _levelManager.LevelLoaded += BindLevel;
            _levelManager.LevelUnloading += ReleaseLevel;
            _cameraRotation.FacingChanged += HandleFacingChanged;
            _characterPanel.OpenChanged += HandleCharacterOpenChanged;
            _portals.VictoryReached += HandleVictoryReached;
            _victoryScreen.Finished += HandleVictoryFinished;
            _victoryScreen.Continued += HandleVictoryContinued;
        }

        private void OnDisable()
        {
            _player.Died -= HandlePlayerDied;
            _mainMenu.Chosen -= HandleMenuChosen;
            _reader.PausePressed -= HandlePausePressed;
            _pauseMenu.PausedChanged -= HandlePausedChanged;
            _pauseMenu.QuitRequested -= HandleQuitToMenu;
            _optionsPanel.LanguageCycleRequested -= CycleLanguage;
            _playerInventory.GoldAdded -= HandleGoldAdded;
            _difficulty.Changed -= ApplyDifficultyToLoot;
            _difficulty.WorldChanged -= HandleWorldChanged;
            _difficultyPanel.SelectRequested -= HandleDifficultySelected;
            _difficultyPanel.NewWorldRequested -= HandleNewWorldRequested;
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
            _levelManager.LevelLoaded -= BindLevel;
            _levelManager.LevelUnloading -= ReleaseLevel;
            _cameraRotation.FacingChanged -= HandleFacingChanged;
            _portals.VictoryReached -= HandleVictoryReached;
            _victoryScreen.Finished -= HandleVictoryFinished;
            _victoryScreen.Continued -= HandleVictoryContinued;
            _characterPanel.OpenChanged -= HandleCharacterOpenChanged;
            ReleaseMerchant();
        }

        // a metà di uno scatto della camera: i lati vicini diventano bassi, quelli lontani alti
        private void HandleFacingChanged(int facing)
        {
            if (_walls != null)
            {
                _walls.Show(facing);
            }
        }

        // dal tasto delle opzioni (D10 della M10), che ha preso il posto di F9 (D13 della M5)
        private void CycleLanguage()
        {
            _localizer.CycleLanguage();
            PlayerPrefs.SetString(LanguagePreference, _localizer.Language);
            PlayerPrefs.Save();
        }

        private void BindLevel(LevelContext level)
        {
            // il player è stato spostato sull'ingresso: senza questo la camera ci arriverebbe con lo
            // smorzamento, attraversando la mappa (trappola 3). Invalidato lo stato, al prossimo
            // LateUpdate si posiziona direttamente sul bersaglio.
            _playerCamera.PreviousStateIsValid = false;
            _walls = level.GetComponent<WallView>();
            HandleFacingChanged(_cameraRotation.Facing);
            _ambience.Play(level.Ambience);
            var tileset = level.Tileset;
            _footsteps.SetSurface(tileset != null ? tileset.Footsteps : null);
            _levelTitle.Show(tileset != null ? tileset.NameKey : null, level.Depth, DifficultyTitleKey());
            _depth = level.Depth;

            // il giorno che passa nel borgo (D5 della M18): l'ora è il tempo di gioco della partita.
            // Camera.main è una ricerca per tag: una volta qui, a ogni livello, mai per frame
            if (level.Daylight != null)
            {
                level.Daylight.Bind(() => _runStats.PlayTime, Camera.main);
            }

            // prima di tutto il resto: i nemici già uccisi non si collegano
            _levelRecorder.Restore(level);
            BindBoss(level);
            foreach (var enemy in level.Enemies)
            {
                // un nemico può essere già morto e sparito se il livello era aperto da prima; il boss
                // già sconfitto è spento e sta per sparire
                if (enemy == null || !enemy.gameObject.activeSelf)
                {
                    continue;
                }

                BindEnemy(enemy, level.Depth, rewarding: true);
            }

            foreach (var chest in level.Chests)
            {
                chest.Bind(_loot, level.Depth);

                // il tesoro del re (D8 della M12): con il re già sconfitto si apre subito
                if (_runStats.BossDefeated)
                {
                    chest.Unseal();
                }
            }

            // i barili da rompere (D5 della M13): il loot con i loro semi, alla profondità del livello
            foreach (var barrel in level.Breakables)
            {
                barrel.Bind(_loot, level.Depth);
            }

            // gli spuntoni (D8 della M13) feriscono il cavaliere, quanto lo dice la profondità
            foreach (var trap in level.Traps)
            {
                trap.Bind(_player, level.Depth);
            }

            PlaceScroll(level);

            _pack = new EnemyPack(level.Enemies);

            // il cavaliere è già sull'ingresso: l'automappa parte scoprendo i suoi dintorni
            _exploration.SetLevel(level.Map, level.Depth);

            // i punti di campioni e unico sull'automappa (D9 della M12), già legati qui sopra
            _specialsOnMap.Clear();
            foreach (var enemy in level.Enemies)
            {
                if (enemy != null && enemy.gameObject.activeSelf && enemy.TryGetComponent(out SpecialEnemy special) && special.Rank != EnemyRank.Boss)
                {
                    _specialsOnMap.Add(special);
                }
            }

            _automap.SetSpecials(_specialsOnMap);
            BindMerchant(level);
            BindAltar(level);
        }

        // il Crepuscolo non si scrive nel titolo: è il gioco di sempre (D15 della M14)
        private string DifficultyTitleKey()
        {
            return _difficulty.Current > 0 ? CurrentDifficulty.NameKey : null;
        }

        private void BindAltar(LevelContext level)
        {
            _altar = level.Altar;
            if (_altar == null)
            {
                return;
            }

            _altar.ShowColor(CurrentDifficulty.Color);
            _altar.Opened += HandleAltarOpened;
        }

        private void ReleaseAltar()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.Close();
            }

            if (_altar != null)
            {
                _altar.Opened -= HandleAltarOpened;
                _altar = null;
            }
        }

        // l'altare chiude gli altri pannelli: si guarda solo lui
        private void HandleAltarOpened()
        {
            _shopPanel.Close();
            _inventoryPanel.SetOpen(false);
            if (_characterPanel.IsOpen)
            {
                _characterPanel.Toggle();
            }

            _difficultyPanel.Open();
        }

        private void HandleDifficultySelected(int index)
        {
            _difficultyPanel.Close();
            _difficulty.Select(index);
        }

        private void HandleNewWorldRequested()
        {
            _difficultyPanel.Close();
            _difficulty.NewWorld();
        }

        // Un mondo nuovo (D4 della M14), per un'altra difficoltà o per "rifai il mondo": un seme ricavato dal
        // vecchio, i ricordi dei livelli, la mappa scoperta e il portale che ripartono, il re vivo con il
        // suo tesoro sigillato. Personaggio, oggetti, oro e contatori restano. Si è in città: nessun
        // livello del mondo vecchio è aperto
        private void HandleWorldChanged(int world)
        {
            ulong seed = SeedMixer.ForWorld(_loot.RunSeed, world);
            _loot.RunSeed = seed;
            _levelManager.RunSeed = seed;
            _levelRecorder.Memory.Import(null);
            _exploration.Memory.Import(null);
            _portals.Forget();
            _runStats.Restore(_runStats.PlayTime, _runStats.Kills, _runStats.Deaths, _runStats.GoldCollected, false);
            _firstVictory = false;
            RestockMerchant();
            if (_altar != null)
            {
                _altar.ShowColor(CurrentDifficulty.Color);
            }

            var level = _levelManager.CurrentLevel;
            if (level != null)
            {
                _levelTitle.Show(level.Tileset != null ? level.Tileset.NameKey : null, level.Depth, DifficultyTitleKey());
            }

            _saveGame.Save();
            Debug.Log($"[DarkDescent] mondo {world} alla difficoltà {_difficulty.Current}: seme {seed}");
        }

        // Un nemico del livello, o un evocato del boss: chi colpire, i tiri, suoni, numeri e barra. Solo
        // quelli del livello danno esperienza e lasciano loot: gli evocati non si allevano (D8 della M10).
        private void BindEnemy(EnemyAI enemy, int depth, bool rewarding)
        {
            enemy.Bind(_player);
            LootBonus bonus = null;
            if (_championSettings != null && enemy.TryGetComponent(out SpecialEnemy special))
            {
                special.Bind(_championSettings, _player, _playerSlow);
                bonus = special.Rank != EnemyRank.Boss ? special.Numbers.Loot : null;
            }

            ApplyDifficulty(enemy);

            if (rewarding)
            {
                enemy.Killed += HandleEnemyKilled;
                _rewardingEnemies.Add(enemy);
            }

            if (enemy.TryGetComponent(out CharacterAudio audio))
            {
                audio.Bind(_sfxLimiter);
            }

            var attack = enemy.GetComponent<MeleeAttack>();
            attack.SetRandomSource(_random);
            if (enemy.TryGetComponent(out RangedAttack ranged))
            {
                ranged.Bind(_projectiles, _random);
                _enemyRanged.Add(ranged);
            }

            _enemyAttacks.Add(attack);
            var health = enemy.GetComponent<Health>();
            _damageNumbers.Track(health, isPlayer: false);
            _enemyBar.Track(health);
            _trackedEnemies.Add(health);
            if (rewarding && enemy.TryGetComponent(out LootDrop loot))
            {
                loot.Bind(_loot, depth, bonus);
            }
        }

        // La difficoltà sopra il grado (D5 della M14): vita e danno moltiplicati, colpire e Armatura in
        // più. Un nemico si lega una volta: ogni livello caricato ha nemici nuovi
        private void ApplyDifficulty(EnemyAI enemy)
        {
            var tier = CurrentDifficulty;
            enemy.GetComponent<Health>().MultiplyMax(tier.Life);
            enemy.GetComponent<MeleeAttack>().SetDifficultyMultiplier(tier.Damage);
            if (enemy.TryGetComponent(out RangedAttack ranged))
            {
                ranged.SetDifficultyMultiplier(tier.Damage);
            }

            if (enemy.TryGetComponent(out CharacterStats stats))
            {
                stats.Sheet.RemoveModifiersFrom(tier);
                stats.Sheet.AddModifier(new StatModifier(StatType.ToHit, ModifierKind.Flat, tier.ToHit, tier));
                stats.Sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, tier.Armor, tier));
            }
        }

        // Il re prende i modificatori dei campioni secondo la difficoltà (D5 della M14), dal seme del
        // mondo. Legato subito: la sua velocità di partenza comprende già il Rapido
        private void PromoteBoss(BossController boss)
        {
            int count = CurrentDifficulty.BossModifiers;
            if (count <= 0 || _championSettings == null || boss.TryGetComponent(out SpecialEnemy _))
            {
                return;
            }

            BossModifiers.Pick(_championSettings.CollectModifierIds(), count, _loot.RunSeed, _bossModifiers);
            var special = boss.gameObject.AddComponent<SpecialEnemy>();
            special.Promote(EnemyRank.Boss, _bossModifiers);
            special.Bind(_championSettings, _player, _playerSlow);
        }

        // Il boss della profondità 8 (D8 della M10): già sconfitto non rinasce (trappola 4), e il
        // livello rigenerato lo toglie prima di legare i nemici
        private void BindBoss(LevelContext level)
        {
            _boss = level.Boss;
            if (_boss == null)
            {
                return;
            }

            if (_runStats.BossDefeated)
            {
                // al suo posto, il portale della vittoria resta aperto (D9)
                _portals.OpenVictoryPortal(VictorySpot(_boss, level), animate: false);
                _boss.gameObject.SetActive(false);
                Destroy(_boss.gameObject);
                _boss = null;
                return;
            }

            PromoteBoss(_boss);
            _boss.Bind(_player, level.BossGate, _random);
            _boss.FightStarted += HandleBossFightStarted;
            _boss.FightReset += HandleBossFightReset;
            _boss.Defeated += HandleBossDefeated;
            _boss.Summoned += HandleBossSummoned;
        }

        private void ReleaseBoss()
        {
            if (!ReferenceEquals(_boss, null))
            {
                _boss.FightStarted -= HandleBossFightStarted;
                _boss.FightReset -= HandleBossFightReset;
                _boss.Defeated -= HandleBossDefeated;
                _boss.Summoned -= HandleBossSummoned;
                _boss = null;
            }

            if (_enemyBar != null)
            {
                _enemyBar.Unpin();
            }
        }

        // la sua vita resta in alto per tutto lo scontro
        private void HandleBossFightStarted(BossController boss)
        {
            _enemyBar.Pin(boss.Health);
            PlayBossMusic(true);
        }

        private void HandleBossFightReset(BossController boss)
        {
            _enemyBar.Unpin();
            PlayBossMusic(false);
        }

        // la musica dello scontro al posto di quella della caverna, e indietro: le due sfumano l'una
        // nell'altra come tra due livelli
        private void PlayBossMusic(bool fighting)
        {
            var level = _levelManager.CurrentLevel;
            if (_bossAmbience != null && level != null)
            {
                _ambience.Play(fighting ? _bossAmbience : level.Ambience);
            }
        }

        // la vittoria va subito nel salvataggio: tornando alla profondità 8 il boss non c'è più. Nella
        // sala si apre il portale della vittoria (D9)
        private void HandleBossDefeated(BossController boss)
        {
            _enemyBar.Unpin();
            PlayBossMusic(false);
            _runStats.MarkBossDefeated();

            // la prima volta a questa difficoltà si apre la successiva, e in città c'è la vittoria; le
            // volte dopo basta una scritta (D15 della M14)
            _firstVictory = _difficulty.CompleteCurrent();
            if (!_firstVictory)
            {
                _hudMessage.Show(TextKeys.KingFallen);
            }

            _saveGame.Save();
            var level = _levelManager.CurrentLevel;
            _portals.OpenVictoryPortal(VictorySpot(boss, level), animate: true);

            // il suo tesoro, in fondo alla sala, ora si apre (D8 della M12)
            if (level != null)
            {
                foreach (var chest in level.Chests)
                {
                    chest.Unseal();
                }
            }
        }

        // Una cella dal centro della sala verso la porta: il bottino cade attorno a dove è morto il re, e
        // dall'altra parte, contro la parete in fondo, c'è il suo tesoro (D8 della M12). Senza porta, a nord.
        private static Vector3 VictorySpot(BossController boss, LevelContext level)
        {
            Vector3 home = boss.GetComponent<EnemyAI>().HomePosition;
            Vector3 toward = Vector3.forward;
            var gate = level != null ? level.BossGate : null;
            if (gate != null)
            {
                Vector3 delta = gate.transform.position - home;
                toward = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) ? new Vector3(Mathf.Sign(delta.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(delta.z));
            }

            return home + toward * LevelMap.CellSize;
        }

        // arrivati in città dal portale della vittoria: il gioco si ferma sulle statistiche
        private void HandleVictoryReached()
        {
            if (!_firstVictory)
            {
                return;
            }

            _firstVictory = false;
            _reader.enabled = false;
            int next = _difficulty.Current + 1;
            _victoryScreen.Show(_runStats, _progress.Progress.Level, next < _difficulties.Count ? _difficulties.Tier(next).NameKey : null);
        }

        // Menu principale dalla vittoria: si salva in città e si torna al menu, con Continua si riprende lì
        private void HandleVictoryFinished()
        {
            _reader.enabled = true;
            HandleQuitToMenu();
        }

        // Continua dalla vittoria: si resta in città a giocare (M17)
        private void HandleVictoryContinued()
        {
            _reader.enabled = true;
        }

        private void HandleBossSummoned(EnemyAI enemy)
        {
            BindEnemy(enemy, _depth, rewarding: false);
            enemy.Alert();
        }

        // la pergamena del livello, se c'è, la porta uno dei nemici o una delle casse (D5 della M9)
        private void PlaceScroll(LevelContext level)
        {
            var scroll = _loot.RollScroll(_scrolls, level.Depth, _scrollWanted, out double carrier);
            if (scroll == null)
            {
                return;
            }

            int carriers = level.Chests.Count;
            foreach (var enemy in level.Enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out LootDrop _))
                {
                    carriers++;
                }
            }

            int pick = Math.Min(carriers - 1, (int)(carrier * carriers));
            foreach (var enemy in level.Enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out LootDrop loot) && pick-- == 0)
                {
                    loot.Carry(scroll);
                    return;
                }
            }

            foreach (var chest in level.Chests)
            {
                if (pick-- == 0)
                {
                    chest.Carry(scroll);
                    return;
                }
            }
        }

        private void ReleaseLevel(LevelContext level)
        {
            _levelRecorder.Remember(level);
            _walls = null;
            _pack?.Release();
            _pack = null;
            foreach (var health in _trackedEnemies)
            {
                _damageNumbers.Untrack(health);
                _enemyBar.Untrack(health);
            }

            _trackedEnemies.Clear();
            _enemyAttacks.Clear();
            _enemyRanged.Clear();

            // anche i nemici già distrutti: l'evento è un campo C#, staccarsi è sempre lecito
            foreach (var enemy in _rewardingEnemies)
            {
                if (!ReferenceEquals(enemy, null))
                {
                    enemy.Killed -= HandleEnemyKilled;
                }
            }

            _rewardingEnemies.Clear();
            _automap.ClearSpecials();
            _specialsOnMap.Clear();
            _exploration.SetLevel(null, 0);
            ReleaseMerchant();
            ReleaseAltar();
            ReleaseBoss();
        }

        // Entrando in città ogni banco tira il suo assortimento: oggetti del livello della profondità
        // più bassa raggiunta, da un seme che cambia con le uccisioni, quindi a ogni ritorno dopo aver
        // combattuto; uguale per lo stesso stato, senza salvarlo (D5 della M10). Ognuno tira solo
        // dai suoi tipi della stessa tabella (D18 della M18)
        private void BindMerchant(LevelContext level)
        {
            foreach (var merchant in level.Merchants)
            {
                _merchants.Add(merchant);
                _shops[merchant] = new Shop(merchant.Goods);
                merchant.Opened += HandleMerchantOpened;
                merchant.Left += HandleMerchantLeft;
            }

            RestockMerchant();
        }

        private void RestockMerchant()
        {
            int depth = Math.Max(1, _exploration.Memory.DeepestDepth);
            foreach (var merchant in _merchants)
            {
                _loot.RollStock(_merchantTable, merchant.Goods, merchant.StockSize, depth, _runStats.Kills, _stock);
                _shops[merchant].Restock(_merchantTable.Supplies, _stock);
            }
        }

        private void ReleaseMerchant()
        {
            // chiudendo la scena il pannello può essere già distrutto
            if (_shopPanel != null)
            {
                _shopPanel.Close();
            }

            foreach (var merchant in _merchants)
            {
                if (merchant != null)
                {
                    merchant.Opened -= HandleMerchantOpened;
                    merchant.Left -= HandleMerchantLeft;
                }
            }

            _merchants.Clear();
            _shops.Clear();
        }

        // il banco prende la metà sinistra, dove sta il pannello del personaggio: uno chiude l'altro
        private void HandleMerchantOpened(Town.Merchant merchant)
        {
            if (_characterPanel.IsOpen)
            {
                _characterPanel.Toggle();
            }

            _shopPanel.Open(_shops[merchant], merchant.NameKey);
        }

        // allontanandosi dal banco il negozio si chiude, e con lui l'inventario che aveva aperto; un
        // altro banco aperto nel frattempo resta
        private void HandleMerchantLeft(Town.Merchant merchant)
        {
            if (!_shopPanel.IsOpen || _shopPanel.Shop != _shops[merchant])
            {
                return;
            }

            _shopPanel.Close();
            _inventoryPanel.SetOpen(false);
        }

        private void HandleCharacterOpenChanged(bool open)
        {
            if (open)
            {
                _shopPanel.Close();
            }
        }

        // l'esperienza del nemico ucciso, alla profondità del livello in cui è morto (D2 della M8)
        private void HandleEnemyKilled(EnemyAI enemy)
        {
            enemy.Killed -= HandleEnemyKilled;
            _rewardingEnemies.Remove(enemy);
            _progress.AwardKill(ExperienceOf(enemy), _depth, CurrentDifficulty);
            _runStats.AddKill();
        }

        // i campioni e gli unici valgono più di un nemico normale dello stesso tipo (M12)
        private static int ExperienceOf(EnemyAI enemy)
        {
            int experience = enemy.Archetype.Experience;
            return enemy.TryGetComponent(out SpecialEnemy special) ? Mathf.RoundToInt(experience * special.ExperienceMultiplier) : experience;
        }

        /// <summary>Cambia il seme della partita: per i test e per rigiocare un seme senza riavviare.</summary>
        public void UseLootSeed(ulong seed)
        {
            _loot.RunSeed = seed;
        }

        /// <summary>Moltiplica le probabilità di unici e set, come <c>-namedchance</c> (D15 della M16): per i test.</summary>
        public void UseNamedChance(float factor)
        {
            _loot.NamedChanceFactor = Mathf.Max(0f, factor);
        }

        /// <summary>
        /// Cambia la sorgente dei tiri per il player e per i nemici del livello, e per quelli dei
        /// livelli successivi. Per i test (una sorgente fissa) e, dalla M5, per rigiocare un seme.
        /// </summary>
        public void UseRandomSource(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _playerAttack.SetRandomSource(random);
            foreach (var attack in _enemyAttacks)
            {
                // un nemico distrutto non tira più
                if (attack != null)
                {
                    attack.SetRandomSource(random);
                }
            }

            foreach (var ranged in _enemyRanged)
            {
                if (ranged != null)
                {
                    ranged.SetRandomSource(random);
                }
            }
        }

        // solo i colpi del player fermano il tempo: con tre scheletri che colpiscono, il gioco singhiozzerebbe
        private void HandlePlayerHitLanded(DamageInfo info)
        {
            if (info.IsCritical)
            {
                _hitStop.TriggerCritical();
                return;
            }

            _hitStop.Trigger();
        }

        // D15 della M7, al posto della ripartenza della M6: il livello resta com'era al momento della
        // morte, con l'inventario, i nemici uccisi, le casse aperte e la mappa scoperta. A schermo nero
        // il cavaliere torna in vita all'ingresso, e i nemici vivi tornano fermi dove li ha messi il
        // livello, a vita piena
        private void Restart()
        {
            // un hit stop o una pausa rimasti a metà farebbero ripartire il gioco rallentato o fermo
            Time.timeScale = 1f;
            _levelManager.ReturnToEntrance(ReviveAndSendEnemiesHome);
        }

        private void ReviveAndSendEnemiesHome()
        {
            _player.Revive();
            foreach (var enemy in _levelManager.CurrentLevel.Enemies)
            {
                // i morti sono già spariti, o stanno per
                if (enemy != null)
                {
                    enemy.ReturnHome();
                }
            }

            // la porta del boss si riapre e lo scontro ricomincia da capo
            if (_boss != null)
            {
                _boss.ResetFight();
            }
        }
    }
}
