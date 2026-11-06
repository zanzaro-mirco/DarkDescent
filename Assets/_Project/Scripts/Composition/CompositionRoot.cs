using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using DarkDescent.Talents;
using DarkDescent.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
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

        [Tooltip("Il lettore dei filmati: bande, didascalie, salto (D9 della M19).")]
        [SerializeField] private Films.FilmPlayer _films;

        [Tooltip("Il set dell'apertura, con la sua Timeline: parte a ogni partita nuova dal menu (D7 della M19).")]
        [SerializeField] private Films.FilmSet _opening;

        [Tooltip("I tre set della vittoria (D8 della M19): il re che cade, il cavaliere che entra nel portale, il ritorno nel borgo.")]
        [SerializeField] private Films.FilmSet _victoryFall;
        [SerializeField] private Films.FilmSet _victoryLeave;
        [SerializeField] private Films.FilmSet _victoryReturn;

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

        [Tooltip("Il pannello delle abilità sul tasto N (D10 della M20).")]
        [SerializeField] private TalentPanel _talentPanel;

        [Tooltip("L'avviso dei punti dell'albero da spendere, accanto alla sfera del mana.")]
        [SerializeField] private TalentPointsButton _talentPointsButton;

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

        [Tooltip("Le barre sopra i nemici feriti, un'opzione (D9 della M23).")]
        [SerializeField] private EnemyHealthBars _healthBars;

        [Tooltip("I suggerimenti del primo gioco (D11 della M23).")]
        [SerializeField] private HintDirector _hints;

        [Tooltip("Il pannello dei comandi, F1 e il pulsante nella pausa (D14 della M23).")]
        [SerializeField] private HelpPanel _helpPanel;

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

        [Tooltip("Il pannello di lettura dei testi del mondo e delle parole del prigioniero (D9 della M25).")]
        [SerializeField] private LorePanel _lorePanel;

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

        // la classe e il nome di una partita nuova, per provarli senza la schermata della creazione (M21)
        private const string ClassOption = "-class";
        private const string HeroNameOption = "-heroname";

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

        /// <summary>"1" fa vedere i filmati anche nell'editor: per i test dei filmati e per le foto.</summary>
        public const string FilmsVariable = "DARKDESCENT_FILMS";

        /// <summary>
        /// Il seme della vittoria rivista dal menu (D18 della M19): la sala del re delle caverne di questo
        /// seme ha la porta, e il cavaliere sta tra la porta e il re.
        /// </summary>
        public const ulong VictoryReplaySeed = 1UL;

        // dove si batte il re delle ossa, per rivedere la vittoria
        private const string KingScene = "Level_Caves";
        private const string KingEntrance = "FromAbove";
        private const int KingDepth = 8;
        // a questa distanza dal re, verso la porta della sala
        private const float KingFacing = 3.5f;

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
        private PlayerTalents _talents;

        // i banchi del borgo, mercante, fabbro e guaritrice, se è il livello corrente (D5 della M10,
        // D18 della M18): un banco per ognuno, con la sua merce
        private readonly List<Town.Merchant> _merchants = new List<Town.Merchant>();
        private readonly Dictionary<Town.Merchant, Shop> _shops = new Dictionary<Town.Merchant, Shop>();
        private readonly List<ItemInstance> _stock = new List<ItemInstance>();

        // l'altare della città, se è il livello corrente (D3 della M14)
        private Town.DifficultyAltar _altar;

        // il re è caduto per la prima volta a questa difficoltà: arrivati in città, la vittoria (D15 della M14)
        private bool _firstVictory;

        // una partita nuova dal menu: arrivati nel borgo, l'apertura (D7 della M19); il set mentre gira
        private bool _openingWanted;
        private Films.FilmSet _set;

        // quello che viene dopo il filmato che gira: il viaggio del portale, la finestra della vittoria
        private Action _afterFilm;

        // un filmato rivisto dal menu (D18 della M19): la partita non si salva e finito il filmato si
        // torna al menu; per la vittoria, arrivati nella sala, il re cade da solo
        private bool _replaying;
        private bool _replayKing;

        // il boss del livello corrente, se c'è e non è ancora stato sconfitto (D8 della M10)
        private BossController _boss;
        private GuardianController _guardian;
        private readonly List<Chest> _guardianChests = new List<Chest>();

        // muri e torce del livello corrente, alti o bassi secondo la camera (D12 della M8); null nei livelli fatti a mano senza
        private WallView _walls;
        private readonly List<EnemyAI> _rewardingEnemies = new List<EnemyAI>();
        private int _depth;

        // il salvataggio da rimettere sul cavaliere in Start, quando tutti ascoltano già (D10 della M8)
        private SaveData _pendingSave;

        // a che difficoltà si gioca (D3 della M14), e il livello di -herolevel da dare in Start (0: niente)
        private DifficultyProgress _difficulty;
        private int _heroLevel;
        private string _classId;
        private string _heroName = "";
        private readonly List<string> _bossModifiers = new List<string>();

        // un generatore solo per tutti i tiri del combattimento, con un seme diverso a ogni avvio (D3)
        private IRandomSource _random;

        private readonly List<MeleeAttack> _enemyAttacks = new List<MeleeAttack>();
        private readonly List<RangedAttack> _enemyRanged = new List<RangedAttack>();

        // i branchi del livello corrente: chi vede il cavaliere sveglia i compagni (D5 della M7)
        private EnemyPack _pack;
        private FleeWatch _fleeWatch;

        // le Health dei nemici seguite dai numeri di danno: al cambio di livello vanno lasciate, anche
        // quelle già distrutte, di cui non si potrebbe più chiedere il componente all'EnemyAI
        private readonly List<Health> _trackedEnemies = new List<Health>();

        /// <summary>La lingua del gioco: i test la leggono e la cambiano da qui.</summary>
        public Localizer Localizer => _localizer;

        public Films.FilmPlayer Films => _films;

        /// <summary>Il seme della partita, da cui viene ogni drop (D5 della M5).</summary>
        public ulong LootSeed => _loot.RunSeed;

        /// <summary>Il livello degli oggetti che cadono a una profondità, con la difficoltà e «Il mondo cresce con te»: per i test.</summary>
        public int LootItemLevel(int depth) => _loot.ItemLevel(depth);

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

            // il livello degli oggetti è quello della zona, con la difficoltà (D7 della M14); con «Il mondo cresce con te» non
            // scende sotto il livello del cavaliere meno il margine (D12 della M24)
            var playerProgress = _player.GetComponent<PlayerProgress>();
            var progression = playerProgress.Settings;
            _loot = new LootRoller(new ItemGenerator(_affixes.Affixes, RarityTable.Default), seed,
                depth => progression.EffectiveLevel(depth, playerProgress.Progress.Level, CurrentDifficulty.ZoneLevelBonus, _difficulty.GrowingWorld));
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
            _healthBars.Bind(_player.transform, _localizer);
            _optionsPanel.Bind(_localizer, _groundLabels, _retroFilter, new[] { _healthOrb.Readout, _manaOrb.Readout }, _healthBars, _hints);
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
            _films.Bind(_reader, _localizer);
            _difficultyPanel.Bind(_difficulties, _difficulty, _localizer);
            _lorePanel.Bind(_localizer);
            var controller = _player.GetComponent<PlayerController>();
            _enemyBar.Bind(controller, _localizer);
            _targetMarker.Bind(controller);
            var magic = _player.GetComponent<Magic.PlayerMagic>();
            _magic = magic;
            magic.Bind(_reader, controller);
            magic.SetPortalOpener(_portals);
            var shrineBonus = _player.GetComponent<ShrineBonus>();
            _hudMessage.Bind(inventory, magic, _localizer, shrineBonus);
            _shrineBonusView.Bind(shrineBonus);
            _manaOrb.Bind(magic.Mana);
            _spellBar.Bind(magic, _localizer);
            if (_spellBar.Menu != null)
            {
                _spellBar.Menu.Bind(magic, _reader, _localizer);
            }
            _portalSlot.Bind(magic);
            // un generatore suo: i danni degli incantesimi non spostano i tiri della mischia (trappola 3 della M9)
            _player.GetComponent<Magic.PlayerSpells>().Bind(_projectiles, new SystemRandomSource(Environment.TickCount ^ 0x5A11));
            // le abilità del Guerriero (M20) tirano con un flusso loro: i colpi normali restano dove sono
            var warrior = _player.GetComponent<Magic.WarriorAbilities>();
            warrior.Bind(new SystemRandomSource(Environment.TickCount ^ 0x4B1D));
            _player.GetComponent<Magic.MageAbilities>().Bind(_projectiles);
            _talents = _player.GetComponent<PlayerTalents>();
            _talentPanel.Bind(_talents, _reader, _localizer);
            // WASD non muove sotto una finestra a tutto schermo (le abilità, l'altare) né mentre un'abilità porta il cavaliere (D8 della M23)
            controller.SetKeyboardBlocker(() => _talentPanel.IsOpen || _difficultyPanel.IsOpen || _helpPanel.IsOpen || _lorePanel.IsOpen || warrior.IsBusy);
            _talentPointsButton.Bind(_talents, _talentPanel, _localizer);
            _hints.Bind(_localizer, _reader, CanShowHints);
            _helpPanel.Bind(_localizer, _reader, _player.GetComponent<PlayerClass>());
            HintSenses.Install(_hints, _levelManager, _player.transform, _progress, _talents, magic, inventory);
            _difficultyPanel.BindTalents(_talents.Build);
            _playerCamera.GetComponent<Rendering.CameraZoom>().Bind(_reader);
        }

        private void Start()
        {
            if (_pendingSave != null)
            {
                _saveGame.Apply(_pendingSave);
                _pendingSave = null;
            }

            ApplyChosenClass();

            if (_heroLevel > 0)
            {
                PrepareHero(_heroLevel);
            }

            if (_saves != null)
            {
                // fermo finché non si sceglie: niente tasti del gioco sotto il menu
                _reader.enabled = false;
                _mainMenu.Show(_saves, _localizer, _difficulties, _player.GetComponent<PlayerClass>().Catalog, _player.gameObject);
            }
        }

        // la classe di -class o della creazione, se ce n'è una: una volta sola, dopo gli Awake
        private void ApplyChosenClass()
        {
            if (_classId == null)
            {
                return;
            }

            var playerClass = _player.GetComponent<PlayerClass>();
            ApplyClass(playerClass.Catalog.Find(_classId), _heroName);
            _classId = null;
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

            _classId = CommandLine.TryGetValue(args, ClassOption, out text) ? text : null;
            _heroName = CommandLine.TryGetValue(args, HeroNameOption, out text) ? text : "";
        }

        /// <summary>
        /// Dà al personaggio la classe e il nome di una partita nuova, con gli attributi di partenza: la
        /// creazione del personaggio e <c>-class</c>. Dopo gli Awake, con gli alberi già collegati.
        /// </summary>
        public void ApplyClass(Classes.CharacterClass characterClass, string characterName)
        {
            _player.GetComponent<PlayerClass>().Apply(characterClass, characterName, true);
        }

        /// <summary>
        /// Il cavaliere di <c>-herolevel</c>: al livello dato, con i punti spesi e un oggetto raro per
        /// slot di quel livello, dal seme della partita, e a vita piena. Anche per i test.
        /// </summary>
        public void PrepareHero(int level)
        {
            HeroSetup.RaiseTo(_progress.Progress, level, _player.GetComponent<PlayerClass>().Current?.PointPattern);
            int equipped = HeroSetup.Equip(_playerInventory.Equipment, _merchantTable.Stock, _loot.Generator, level, _loot.RunSeed,
                heroClass: _player.GetComponent<PlayerClass>().Current);
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
            BeginGame(slot, isNew, null, null);
        }

        /// <summary>
        /// Un personaggio nuovo dalla creazione (D8 della M21): la classe e il nome scelti, nel posto scelto.
        /// Si applicano prima di tutto il resto (il livello di <c>-herolevel</c>, l'apertura): i punti, l'equipaggiamento e il
        /// corpo che il filmato copia sono quelli della classe.
        /// </summary>
        private void HandleMenuCreated(int slot, string classId, string heroName)
        {
            BeginGame(slot, true, classId, heroName);
        }

        private void BeginGame(int slot, bool isNew, string classId, string heroName)
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
                _openingWanted = FilmsWanted();
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
                // la scelta della creazione vale più di -class
                if (classId != null)
                {
                    _classId = classId;
                    _heroName = heroName;
                }

                ApplyChosenClass();
                if (_heroLevel > 0)
                {
                    PrepareHero(_heroLevel);
                }
            }

            _mainMenu.Hide();
            _reader.enabled = true;
            _levelManager.Begin();
        }

        /// <summary>
        /// Un filmato rivisto dal menu (D18 della M19), in una partita che non si salva, con il cavaliere
        /// di base. L'apertura nel borgo, come in una partita nuova; la vittoria nella sala del re di un
        /// seme fisso, con il re che cade appena si arriva. Finito o saltato il filmato, si torna al menu.
        /// </summary>
        private void HandleFilmChosen(string id)
        {
            _replaying = true;
            _runStats.Restore(0f, 0, 0, 0, false);
            _difficulty.Restore(0, -1, 0);
            ulong seed = (ulong)DateTime.UtcNow.Ticks;
            if (id == DarkDescent.Films.FilmLibrary.VictoryFallId)
            {
                seed = VictoryReplaySeed;
                _replayKing = true;
                _levelManager.SetStartLevel(KingScene, KingEntrance, KingDepth);
                _portals.Restore(0);
            }
            else
            {
                StartInTown();
                _openingWanted = true;
            }

            _loot.RunSeed = seed;
            _levelManager.RunSeed = seed;
            Debug.Log($"[DarkDescent] filmato rivisto dal menu: {id}");
            _mainMenu.Hide();
            _reader.enabled = true;
            _levelManager.Begin();
        }

        // il filmato rivisto è finito: si ricarica la scena, che riparte dal menu, senza salvare
        private void BackToMenu()
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }

        // il cavaliere davanti al re, verso la porta, e appena finita la dissolvenza il re cade
        private IEnumerator ReplayKing(LevelContext level)
        {
            var boss = level.Boss;
            Vector3 home = boss.GetComponent<EnemyAI>().HomePosition;
            Vector3 toward = Vector3.forward;
            if (level.BossGate != null)
            {
                toward = level.BossGate.transform.position - home;
                toward.y = 0f;
                toward.Normalize();
            }

            if (NavMesh.SamplePosition(home + toward * KingFacing, out var hit, 2f, NavMesh.AllAreas)
                && _player.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }

            _player.transform.rotation = Quaternion.LookRotation(-toward);
            while (_levelManager.IsTransitioning)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
            if (boss != null && !boss.Health.IsDead)
            {
                boss.Health.TakeDamage(new DamageInfo(boss.Health.Max * 10f, DamageType.Physical, _player.gameObject));
            }
        }

        // F1: il pannello dei comandi, anche in pausa; non dal menu iniziale né durante un passaggio di livello
        private void HandleHelpToggled()
        {
            if (_mainMenu.IsShown || _player.IsDead || (_films != null && _films.IsPlaying))
            {
                return;
            }

            // in pausa il pannello passa dal menu, che gli lascia il posto e poi torna
            if (_helpPanel.IsOpen)
            {
                _helpPanel.Close();
            }
            else if (_pauseMenu.IsOpen)
            {
                _pauseMenu.ShowHelp();
            }
            else
            {
                _helpPanel.Open();
            }
        }

        // Esc: prima chiude le finestre aperte, poi apre e chiude la pausa (D10 della M10)
        private void HandlePausePressed()
        {
            if (_mainMenu.IsShown)
            {
                return;
            }

            // il pannello dei comandi si chiude per primo: se veniva dalla pausa, la pausa torna
            if (_helpPanel.IsOpen)
            {
                _helpPanel.Close();
                return;
            }

            if (_lorePanel.IsOpen)
            {
                _lorePanel.Close();
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

            if (_talentPanel.IsOpen)
            {
                _talentPanel.Close();
                return;
            }

            // il menu delle abilità (D18 della M20) si chiude da solo, come la difficoltà
            if (_spellBar.Menu != null && _spellBar.Menu.IsOpen)
            {
                _spellBar.Menu.Close();
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

        // i suggerimenti (D12 della M23) tacciono quando il gioco non è da guardare: menu, pausa, filmati, passaggi di livello, morte, sala del re, finestre a tutto schermo
        private bool CanShowHints()
        {
            return _levelManager.CurrentLevel != null
                && !_levelManager.IsTransitioning
                && !_player.IsDead
                && !_mainMenu.IsShown
                && !_pauseMenu.IsOpen
                && !_talentPanel.IsOpen
                && !_difficultyPanel.IsOpen
                && !_helpPanel.IsOpen
                && !_lorePanel.IsOpen
                && !(_films != null && _films.IsPlaying)
                && !(_boss != null && _boss.IsFighting)
                && !(_guardian != null && _guardian.IsFighting);
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
            // durante un filmato, e finché la sua musica suona, la musica del livello tace (D10 della M19)
            if (_films != null)
            {
                _ambience.Hushed = _films.IsPlaying || _films.IsMusicPlaying;
            }

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
            _mainMenu.Created += HandleMenuCreated;
            _mainMenu.FilmChosen += HandleFilmChosen;
            _reader.PausePressed += HandlePausePressed;
            _reader.HelpToggled += HandleHelpToggled;
            _pauseMenu.PausedChanged += HandlePausedChanged;
            _pauseMenu.QuitRequested += HandleQuitToMenu;
            _optionsPanel.LanguageCycleRequested += CycleLanguage;
            _playerInventory.GoldAdded += HandleGoldAdded;
            _difficulty.Changed += ApplyDifficultyToLoot;
            _difficulty.WorldChanged += HandleWorldChanged;
            _difficultyPanel.SelectRequested += HandleDifficultySelected;
            _difficultyPanel.NewWorldRequested += HandleNewWorldRequested;
            _difficultyPanel.GrowingWorldRequested += HandleGrowingWorldRequested;
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
            _levelManager.LevelLoaded += BindLevel;
            _levelManager.LevelUnloading += ReleaseLevel;
            _cameraRotation.FacingChanged += HandleFacingChanged;
            _characterPanel.OpenChanged += HandleCharacterOpenChanged;
            _talentPanel.OpenChanged += HandleTalentsOpenChanged;
            _difficultyPanel.ResetTalentsRequested += HandleResetTalents;
            _portals.VictoryReached += HandleVictoryReached;
            _portals.VictoryEntered += HandleVictoryEntered;
            _victoryScreen.Finished += HandleVictoryFinished;
            _victoryScreen.Continued += HandleVictoryContinued;
        }

        private void OnDisable()
        {
            _player.Died -= HandlePlayerDied;
            _mainMenu.Chosen -= HandleMenuChosen;
            _mainMenu.Created -= HandleMenuCreated;
            _mainMenu.FilmChosen -= HandleFilmChosen;
            _reader.PausePressed -= HandlePausePressed;
            _reader.HelpToggled -= HandleHelpToggled;
            _pauseMenu.PausedChanged -= HandlePausedChanged;
            _pauseMenu.QuitRequested -= HandleQuitToMenu;
            _optionsPanel.LanguageCycleRequested -= CycleLanguage;
            _playerInventory.GoldAdded -= HandleGoldAdded;
            _difficulty.Changed -= ApplyDifficultyToLoot;
            _difficulty.WorldChanged -= HandleWorldChanged;
            _difficultyPanel.SelectRequested -= HandleDifficultySelected;
            _difficultyPanel.NewWorldRequested -= HandleNewWorldRequested;
            _difficultyPanel.GrowingWorldRequested -= HandleGrowingWorldRequested;
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
            _levelManager.LevelLoaded -= BindLevel;
            _levelManager.LevelUnloading -= ReleaseLevel;
            _cameraRotation.FacingChanged -= HandleFacingChanged;
            _portals.VictoryReached -= HandleVictoryReached;
            _portals.VictoryEntered -= HandleVictoryEntered;
            _victoryScreen.Finished -= HandleVictoryFinished;
            _victoryScreen.Continued -= HandleVictoryContinued;
            _characterPanel.OpenChanged -= HandleCharacterOpenChanged;
            _talentPanel.OpenChanged -= HandleTalentsOpenChanged;
            _difficultyPanel.ResetTalentsRequested -= HandleResetTalents;
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

            // la stanza sigillata (D5 della M25) si mette prima del ricordo: il ricordo la ritrova tirata e aperta
            var sealedRoom = ArmSealedRoom(level);
            ArmLore(level, sealedRoom);
            ArmStatue(level, sealedRoom);

            // prima di tutto il resto: i nemici già uccisi non si collegano
            _levelRecorder.Restore(level);
            BindBoss(level);
            BindGuardian(level);
            foreach (var enemy in level.Enemies)
            {
                // un nemico può essere già morto e sparito se il livello era aperto da prima; il boss
                // già sconfitto è spento e sta per sparire
                if (enemy == null || !enemy.gameObject.activeSelf)
                {
                    continue;
                }

                BindEnemy(enemy, level.Depth, rewarding: true);

                // chi sta nel fondo chiuso dalla porta non fa la ronda: i suoi punti potrebbero stare oltre la porta
                if (sealedRoom == null || sealedRoom.IsDone || !sealedRoom.Holds(enemy.HomePosition))
                {
                    enemy.BindPatrol(level.Depth);
                }
            }

            foreach (var chest in level.Chests)
            {
                chest.Bind(_loot, level.Depth);

                // il tesoro del re (D8 della M12): con il re già sconfitto si apre subito. Quello del Custode no: ha il suo
                if (_runStats.BossDefeated && !chest.OpensWithGuardian)
                {
                    chest.Unseal();
                }
            }

            ArmChestSecrets(level);

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

            _pack = new EnemyPack(level.Enemies);
            _fleeWatch = new FleeWatch(level.Enemies);

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

            if (_openingWanted && level.IsTown)
            {
                _openingWanted = false;
                PlayFilm(_opening, level, _replaying ? (Action)BackToMenu : null);
            }

            if (_replayKing && level.Boss != null)
            {
                _replayKing = false;
                StartCoroutine(ReplayKing(level));
            }
        }

        /// <summary>Il filmato che gira adesso, con il suo set; null se non ce n'è.</summary>
        public Films.FilmSet CurrentFilm => _set;

        // i filmati si vedono nelle build; nell'editor, e quindi nei test, solo se un test lo chiede, o
        // se si rivedono dal menu
        private bool FilmsWanted()
        {
            return _replaying || !Application.isEditor || Environment.GetEnvironmentVariable(FilmsVariable) == "1";
        }

        // Il set si monta nel livello appena arrivato e si smonta quando il filmato finisce o si salta;
        // poi <paramref name="then"/>, che c'è anche se il filmato non può partire
        private void PlayFilm(Films.FilmSet prefab, LevelContext level, Action then = null)
        {
            if (prefab == null || _set != null || level == null)
            {
                then?.Invoke();
                return;
            }

            _afterFilm = then;
            _set = Instantiate(prefab);
            _set.Build(level, _player.gameObject, _playerCamera);
            _films.Finished += HandleFilmFinished;
            if (!_films.Play(_set.Script, _set.Director, _set.Music))
            {
                HandleFilmFinished(_set.Script);
            }
        }

        private void HandleFilmFinished(Films.FilmScript script)
        {
            _films.Finished -= HandleFilmFinished;
            if (_set != null)
            {
                _set.Teardown();
                Destroy(_set.gameObject);
                _set = null;
            }

            var then = _afterFilm;
            _afterFilm = null;
            then?.Invoke();
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
            _talentPanel.Close();
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

        // l'interruttore «Il mondo cresce con te» (D12 della M24): niente conferma e niente mondo nuovo, si può girare in ogni momento
        private void HandleGrowingWorldRequested(bool on)
        {
            _difficulty.SetGrowingWorld(on);
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

            ApplyDifficulty(enemy, depth);

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
            _healthBars.Track(health);
            _trackedEnemies.Add(health);
            if (rewarding && enemy.TryGetComponent(out LootDrop loot))
            {
                loot.Bind(_loot, depth, bonus);
            }
        }

        // La difficoltà sopra il grado (D5 della M14): vita e danno moltiplicati, colpire e Armatura in
        // più. Un nemico si lega una volta: ogni livello caricato ha nemici nuovi
        private void ApplyDifficulty(EnemyAI enemy, int depth)
        {
            var tier = CurrentDifficulty;
            float growth = GrowthOf(enemy, depth);
            enemy.GetComponent<Health>().MultiplyMax(tier.Life * growth);
            enemy.GetComponent<MeleeAttack>().SetDifficultyMultiplier(tier.Damage * growth);
            if (enemy.TryGetComponent(out RangedAttack ranged))
            {
                ranged.SetDifficultyMultiplier(tier.Damage * growth);
            }

            if (enemy.TryGetComponent(out CharacterStats stats))
            {
                stats.Sheet.RemoveModifiersFrom(tier);
                stats.Sheet.AddModifier(new StatModifier(StatType.ToHit, ModifierKind.Flat, tier.ToHit, tier));
                stats.Sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, tier.Armor, tier));
            }
        }

        // «Il mondo cresce con te» (D12–D14 della M24): vita e danno seguono il livello efficace; il re e il Custode hanno la loro
        // taratura e non crescono, i campioni e gli unici sì (il grado si applica sopra). Un nemico si lega una volta, al caricamento
        private float GrowthOf(EnemyAI enemy, int depth)
        {
            if (!_difficulty.GrowingWorld || enemy.TryGetComponent(out BossController _) || enemy.TryGetComponent(out GuardianController _))
            {
                return 1f;
            }

            var progress = _player.GetComponent<PlayerProgress>();
            return progress.Settings.EnemyGrowth(depth, progress.Progress.Level, CurrentDifficulty.ZoneLevelBonus, true);
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
                _portals.HoldVictory = _firstVictory && FilmsWanted();
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

        // Vorrak il Custode (D8–D11 della M24): già sconfitto non rinasce (il ricordo del livello lo toglie prima di legare i nemici, e il suo
        // tesoro si apre subito); vivo, la sua vita sta in alto per tutto lo scontro e il suo tesoro aspetta
        private void BindGuardian(LevelContext level)
        {
            _guardian = level.Guardian;
            bool alive = _guardian != null && _guardian.gameObject.activeSelf && !_guardian.Health.IsDead;
            foreach (var chest in level.Chests)
            {
                if (chest.OpensWithGuardian && !alive)
                {
                    chest.Unseal();
                }
            }

            if (!alive)
            {
                _guardian = null;
                return;
            }

            _guardian.Bind(_player, _random);
            _guardian.FightStarted += HandleGuardianFightStarted;
            _guardian.FightReset += HandleGuardianFightReset;
            _guardian.Defeated += HandleGuardianDefeated;
            _guardian.Summoned += HandleBossSummoned;
            _guardianChests.Clear();
            _guardianChests.AddRange(level.Chests.Where(c => c.OpensWithGuardian));
        }

        private void ReleaseGuardian()
        {
            if (!ReferenceEquals(_guardian, null))
            {
                _guardian.FightStarted -= HandleGuardianFightStarted;
                _guardian.FightReset -= HandleGuardianFightReset;
                _guardian.Defeated -= HandleGuardianDefeated;
                _guardian.Summoned -= HandleBossSummoned;
                _guardian = null;
            }

            _guardianChests.Clear();
            if (_enemyBar != null)
            {
                _enemyBar.Unpin();
            }
        }

        private void HandleGuardianFightStarted(GuardianController guardian)
        {
            _enemyBar.Pin(guardian.Health);
        }

        private void HandleGuardianFightReset(GuardianController guardian)
        {
            _enemyBar.Unpin();
        }

        // il suo tesoro si apre, nella sala; niente portale né vittoria: la scala è già lì
        private void HandleGuardianDefeated(GuardianController guardian)
        {
            _enemyBar.Unpin();
            foreach (var chest in _guardianChests)
            {
                if (chest != null)
                {
                    chest.Unseal();
                }
            }
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

            // la prima volta, il primo tempo della vittoria; il portale aspetta il secondo (D8 della M19)
            bool films = _firstVictory && FilmsWanted();
            _portals.HoldVictory = films;
            if (films)
            {
                // rivista dal menu, il cavaliere non va al portale: il secondo pezzo segue subito
                PlayFilm(_victoryFall, level, _replaying ? (Action)HandleVictoryEntered : null);
            }
        }

        // nel portale della vittoria, la prima volta: il cavaliere guarda le ossa del re, poi il viaggio
        private void HandleVictoryEntered()
        {
            PlayFilm(_victoryLeave, _levelManager.CurrentLevel, _portals.TravelToVictory);
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
            if (FilmsWanted())
            {
                // vista in una partita, la vittoria si può rivedere dal menu (D18 della M19)
                if (!_replaying)
                {
                    FilmsPanel.MarkVictorySeen();
                }

                PlayFilm(_victoryReturn, _levelManager.CurrentLevel, _replaying ? (Action)BackToMenu : ShowVictory);
                return;
            }

            ShowVictory();
        }

        private void ShowVictory()
        {
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

        // La stanza sigillata (D5 della M25): un fondo cieco del livello che c'è già, chiuso da una porta di pietra con la leva altrove. Non nasce niente
        // nella mappa: le casse e i nemici di quel fondo sono il tesoro e la scorta, e il ricordo del livello combacia come prima. Si registra tra i
        // segreti del livello, prima che il ricordo venga rimesso
        private SealedRoom ArmSealedRoom(LevelContext level)
        {
            var tileset = level.Tileset;
            if (!SealedRoom.Enabled || level.IsTown || level.Map == null || tileset == null || tileset.SealedDoor == null || tileset.Lever == null)
            {
                return null;
            }

            var plan = SealedRoomPlanner.For(level.Map, _loot.RunSeed, level.Depth);
            if (plan == null)
            {
                return null;
            }

            var room = SealedRoom.Build(level, plan, tileset.SealedDoor, tileset.Lever);
            room.Unlocked += HandleSealedRoomUnlocked;
            room.Lever.BindListener(_player.transform);
            level.RegisterSecret(room);
            return room;
        }

        // «Hai sentito scattare qualcosa» (D12 della M25): la leva è stata tirata e la porta è lontana, forse fuori vista
        private void HandleSealedRoomUnlocked()
        {
            _hudMessage.Show(TextKeys.SecretHeard);
        }

        // Il testo del mondo del livello (D9 della M25): un leggio, una lapide o delle carte contro un muro, due livelli su tre. Il testo e il posto sono una
        // funzione del seme del mondo, della profondità e della mappa, come per la stanza sigillata: il ricordo del livello trova le stesse cose nello stesso ordine.
        // Va registrato dopo la stanza sigillata e prima di prigioniero e statua (stesso ordine a ogni caricamento)
        private void ArmLore(LevelContext level, SealedRoom sealedRoom)
        {
            if (!LoreSpot.Enabled || level.IsTown || level.Map == null || level.Tileset == null)
            {
                return;
            }

            int text = LoreRules.TextFor(_loot.RunSeed, level.Depth);
            var prefab = text > 0 ? level.Tileset.LoreProp(LoreRules.KindOf(text)) : null;
            if (prefab == null)
            {
                return;
            }

            var avoid = new List<Vector2Int>();
            HashSet<Vector2Int> exclude = null;
            if (sealedRoom != null)
            {
                avoid.Add(sealedRoom.Plan.Door);
                avoid.Add(sealedRoom.Plan.Lever);
                exclude = new HashSet<Vector2Int>(sealedRoom.Plan.Region);
            }

            var spot = SecretSpots.Pick(level.Map, _loot.RunSeed, level.Depth, LoreRules.SpotKind, avoid, exclude);
            if (spot == null)
            {
                return;
            }

            var lore = LoreSpot.Build(level, prefab, spot.Value, text);
            lore.Reading += HandleLoreReading;
            level.RegisterSecret(lore);
        }

        // La statua che si anima (D7 della M25): vicino a una cassa normale scelta dal seme tra tutte quelle del livello (anche le aperte), mai un mimic, mai il baule
        // maledetto né una cassa a meno di 14 m da lui, mai nel fondo della stanza sigillata. Il tetto di tre segreti (D16): se il livello ha già stanza sigillata, prigioniero
        // e baule maledetto, la statua cede. Tutto è una funzione del seme e delle casse, anche con gli interruttori spenti: il livello è lo stesso e l'ordine dei segreti pure
        private void ArmStatue(LevelContext level, SealedRoom sealedRoom)
        {
            if (!StatueGuard.Enabled || level.IsTown || level.Map == null || level.Tileset == null || level.Tileset.Statue == null
                || !StatueRules.LevelWantsOne(_loot.RunSeed, level.Depth))
            {
                return;
            }

            var chests = new List<Chest>();
            var cells = new List<Vector2Int>();
            foreach (var chest in level.Chests)
            {
                if (!chest.OpensWithBoss && !chest.OpensWithGuardian && chest.TryGetComponent(out ChestSecret _))
                {
                    chests.Add(chest);
                    cells.Add(chest.Cell);
                }
            }

            int cursed = SecretRules.PickCursed(_loot.RunSeed, level.Depth, cells);
            if (StatueRules.Yields(sealedRoom != null, false, cursed >= 0))
            {
                return;
            }

            var eligible = new List<int>();
            var eligibleCells = new List<Vector2Int>();
            for (int i = 0; i < chests.Count; i++)
            {
                bool mimic = SecretRules.IsMimic(_loot.RunSeed, level.Depth, cells[i].x, cells[i].y);
                bool nearCursed = cursed >= 0 && Vector3.Distance(chests[i].transform.position, chests[cursed].transform.position) < StatueRules.CursedSeparation;
                bool sealedIn = sealedRoom != null && sealedRoom.Touches(chests[i].transform.position);
                if (!mimic && !nearCursed && !sealedIn)
                {
                    eligible.Add(i);
                    eligibleCells.Add(cells[i]);
                }
            }

            int pick = StatueRules.Pick(_loot.RunSeed, level.Depth, eligibleCells);
            if (pick < 0)
            {
                return;
            }

            var guarded = chests[eligible[pick]];
            var guard = StatueGuard.Build(level, level.Tileset.Statue, StatueSpot(level.Map, guarded), guarded.transform.position);
            guard.BindPlayer(_player.transform);
            guard.Spawned += HandleSecretSpawned;
            level.RegisterSecret(guard);
        }

        // il posto della statua: accanto al davanti della cassa, su pavimento semplice della mappa, di lato e poi un poco avanti; senza un posto di lato, davanti
        private static Vector3 StatueSpot(LevelMap map, Chest chest)
        {
            var t = chest.transform;
            Vector3 front = chest.Front;
            var candidates = new[]
            {
                front + t.right * 2.6f,
                front - t.right * 2.6f,
                front + t.right * 2.6f + t.forward * 1.5f,
                front - t.right * 2.6f + t.forward * 1.5f,
                front + t.forward * 2.4f,
            };
            var walkable = SealedRoomPlanner.Walkability(map);
            foreach (var candidate in candidates)
            {
                int x = Mathf.RoundToInt(candidate.x / LevelMap.CellSize);
                int y = Mathf.RoundToInt(-candidate.z / LevelMap.CellSize);
                if (x > 0 && y > 0 && x < map.Width - 1 && y < map.Height - 1 && map.GetSymbol(x, y) == LevelMap.Floor && walkable[x, y])
                {
                    return candidate;
                }
            }

            return candidates[candidates.Length - 1];
        }

        private void HandleLoreReading(LoreSpot lore)
        {
            _lorePanel.Open(LoreRules.TitleKey(lore.TextId), LoreRules.BodyKey(lore.TextId));
        }

        // I segreti delle casse normali (D3 e D4 della M25): decisi qui, sulle casse che il livello ha già, e non dalla mappa: il numero e
        // l'ordine di casse e nemici restano quelli del livello generato, e un ricordo di prima (ADR-079) combacia ancora. Tutte le casse
        // normali contano per la scelta del baule maledetto, anche quelle già aperte, così aprirne una non sposta la scelta
        private void ArmChestSecrets(LevelContext level)
        {
            if (!ChestSecret.Enabled || level.IsTown)
            {
                return;
            }

            var secrets = new List<ChestSecret>();
            var cells = new List<Vector2Int>();
            foreach (var chest in level.Chests)
            {
                if (chest.OpensWithBoss || chest.OpensWithGuardian || !chest.TryGetComponent(out ChestSecret secret))
                {
                    continue;
                }

                secrets.Add(secret);
                cells.Add(chest.Cell);
            }

            int cursed = SecretRules.PickCursed(_loot.RunSeed, level.Depth, cells);
            for (int i = 0; i < secrets.Count; i++)
            {
                var chest = secrets[i].Chest;
                if (chest.IsOpen)
                {
                    continue;
                }

                var kind = i == cursed ? ChestSecretKind.Cursed
                    : SecretRules.IsMimic(_loot.RunSeed, level.Depth, cells[i].x, cells[i].y) ? ChestSecretKind.Mimic : ChestSecretKind.None;
                if (kind == ChestSecretKind.None)
                {
                    continue;
                }

                chest.ArmSecret(kind);
                secrets[i].Arm(kind, _loot.RunSeed, level.Depth);
                secrets[i].Spawned += HandleSecretSpawned;
                if (kind == ChestSecretKind.Cursed)
                {
                    // il premio della profondità dopo, con il suo bonus; l'etichetta dice cos'è (D4)
                    chest.Bind(_loot, level.Depth + 1);
                    chest.SetBonus(ChestSecret.CursedBonus);
                    chest.GetComponent<DarkDescent.Interaction.Interactable>().SetLabel("chest.cursed", null);
                }
            }
        }

        // un nemico nato da un segreto (il mimic, l'ondata del baule maledetto): vita, danno, esperienza e barra come gli altri, e subito all'attacco
        private void HandleSecretSpawned(EnemyAI enemy)
        {
            BindEnemy(enemy, _depth, rewarding: true);
            enemy.Alert();
        }

        private void HandleBossSummoned(EnemyAI enemy)
        {
            BindEnemy(enemy, _depth, rewarding: false);
            enemy.Alert();
        }

        private void ReleaseLevel(LevelContext level)
        {
            _levelRecorder.Remember(level);
            _lorePanel.Close();
            _walls = null;
            _pack?.Release();
            _pack = null;
            _fleeWatch?.Release();
            _fleeWatch = null;
            foreach (var health in _trackedEnemies)
            {
                _damageNumbers.Untrack(health);
                _enemyBar.Untrack(health);
                _healthBars.Untrack(health);
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
            ReleaseGuardian();
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
                _talentPanel.Close();
            }
        }

        // le abilità a tutto schermo: si guardano da sole, il resto si chiude
        private void HandleTalentsOpenChanged(bool open)
        {
            if (!open)
            {
                return;
            }

            _shopPanel.Close();
            _inventoryPanel.SetOpen(false);
            if (_characterPanel.IsOpen)
            {
                _characterPanel.Toggle();
            }

            if (_spellBar.Menu != null)
            {
                _spellBar.Menu.Close();
            }
        }

        // l'altare ricomincia l'albero (D11 della M20): 30 oro per livello, e tutti i punti tornano
        private void HandleResetTalents()
        {
            _difficultyPanel.Close();
            var build = _talents.Build;
            if (!_playerInventory.TrySpendGold(TalentPoints.ResetCost(build.Level)))
            {
                _hudMessage.Show(TextKeys.AltarTalentsPoor);
                return;
            }

            build.Reset();
            _hudMessage.Show(TextKeys.AltarTalentsDone);
        }

        // l'esperienza del nemico ucciso, alla profondità del livello in cui è morto (D2 della M8)
        private void HandleEnemyKilled(EnemyAI enemy)
        {
            enemy.Killed -= HandleEnemyKilled;
            _rewardingEnemies.Remove(enemy);
            _progress.AwardKill(ExperienceOf(enemy), _depth, CurrentDifficulty, _difficulty.GrowingWorld);
            _runStats.AddKill();
            _talents.HandleKill();
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

            if (_guardian != null)
            {
                _guardian.ResetFight();
            }
        }
    }
}
