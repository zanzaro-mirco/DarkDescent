using System;
using System.Collections.Generic;
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
using DarkDescent.UI;
using Unity.Cinemachine;
using UnityEngine;

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

        [SerializeField] private LevelManager _levelManager;

        [SerializeField] private HealthOrb _healthOrb;

        [SerializeField] private DeathScreen _deathScreen;

        [SerializeField] private DamageNumbers _damageNumbers;

        [SerializeField] private InteractableLabel _interactableLabel;

        [SerializeField] private InventoryPanel _inventoryPanel;

        [SerializeField] private CharacterPanel _characterPanel;

        [SerializeField] private BeltView _beltView;

        [SerializeField] private ExplorationTracker _exploration;

        [SerializeField] private AutomapView _automap;

        [SerializeField] private ItemCursor _itemCursor;

        [SerializeField] private HitStop _hitStop;

        [SerializeField] private CinemachineCamera _playerCamera;

        [Tooltip("Musica, fondo e versi nel buio: cambiano con il livello (passo 7.0 della M7).")]
        [SerializeField] private AmbiencePlayer _ambience;

        [Tooltip("Il limite di voci dei personaggi (D9 della M7): cavaliere e nemici gli chiedono il permesso.")]
        [SerializeField] private SfxLimiter _sfxLimiter;

        [Tooltip("La scritta del livello in cui si è: grande entrando, piccola sotto la minimappa.")]
        [SerializeField] private LevelTitle _levelTitle;

        [SerializeField] private InventoryFullMessage _inventoryFullMessage;

        [SerializeField] private ExperienceBar _experienceBar;

        [SerializeField] private SaveGame _saveGame;

        [Tooltip("Nome e vita del nemico sotto il cursore, in alto al centro.")]
        [SerializeField] private EnemyBar _enemyBar;

        [Tooltip("Il cerchio rosso a terra sotto il nemico che ha il cursore addosso.")]
        [SerializeField] private Rendering.TargetMarker _targetMarker;

        [Tooltip("La tabella delle stringhe: una colonna per lingua (D12 della M5).")]
        [SerializeField] private TextAsset _strings;

        [Tooltip("Gli affissi che il generatore può tirare (D3 della M5).")]
        [SerializeField] private AffixDatabase _affixes;

        /// <summary>Dove resta la lingua scelta tra un avvio e l'altro: è una preferenza, non la partita.</summary>
        public const string LanguagePreference = "language";

        private const string LanguageOption = "-lang";

        private const string SeedOption = "-seed";

        // ignora il salvataggio e comincia una partita nuova (D10 della M8), finché non c'è il menu della M10
        private const string NewGameOption = "-newgame";

        private MeleeAttack _playerAttack;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private LootRoller _loot;
        private Footsteps _footsteps;
        private PlayerProgress _progress;
        private Rendering.CameraRotation _cameraRotation;

        // muri e torce del livello corrente, alti o bassi secondo la camera (D12 della M8); null nei livelli fatti a mano senza
        private WallView _walls;
        private readonly List<EnemyAI> _rewardingEnemies = new List<EnemyAI>();
        private int _depth;

        // il salvataggio da rimettere sul cavaliere in Start, quando tutti ascoltano già (D10 della M8)
        private SaveData _pendingSave;

        // un generatore solo per tutti i tiri del combattimento, con un seme diverso a ogni avvio (D3)
        private IRandomSource _random;

        private readonly List<MeleeAttack> _enemyAttacks = new List<MeleeAttack>();

        // i branchi del livello corrente: chi vede il cavaliere sveglia i compagni (D5 della M7)
        private EnemyPack _pack;

        // le Health dei nemici seguite dai numeri di danno: al cambio di livello vanno lasciate, anche
        // quelle già distrutte, di cui non si potrebbe più chiedere il componente all'EnemyAI
        private readonly List<Health> _trackedEnemies = new List<Health>();

        /// <summary>La lingua del gioco: i test la leggono e la cambiano da qui.</summary>
        public Localizer Localizer => _localizer;

        /// <summary>Il seme della partita, da cui viene ogni drop (D5 della M5).</summary>
        public ulong LootSeed => _loot.RunSeed;

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
            string[] args = Environment.GetCommandLineArgs();
            bool seedGiven = CommandLine.TryGetValue(args, SeedOption, out string seedText) && ulong.TryParse(seedText, out _);
            if (!seedGiven || !ulong.TryParse(seedText, out ulong seed))
            {
                seed = (ulong)DateTime.UtcNow.Ticks;
            }

            // un salvataggio, se c'è, decide seme e livello (D10 della M8); -newgame e -seed lo ignorano
            _saveGame.Bind(_levelManager, _player.gameObject, _exploration);
            if (!seedGiven && !CommandLine.HasFlag(args, NewGameOption))
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
            }

            _loot = new LootRoller(new ItemGenerator(_affixes.Affixes, RarityTable.Default), seed);
            // lo stesso seme fa anche i livelli generati: ognuno ne ricava uno suo (D8 della M6)
            _levelManager.RunSeed = seed;
            Debug.Log($"[DarkDescent] seme della partita: {seed} (per rigiocarla: -seed {seed})");

            _playerAttack = _player.GetComponent<MeleeAttack>();
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
            _inventoryFullMessage.Bind(inventory, _localizer);
            var controller = _player.GetComponent<PlayerController>();
            _enemyBar.Bind(controller, _localizer);
            _targetMarker.Bind(controller);
            _playerCamera.GetComponent<Rendering.CameraZoom>().Bind(_reader);
        }

        private void Start()
        {
            if (_pendingSave != null)
            {
                _saveGame.Apply(_pendingSave);
                _pendingSave = null;
            }
        }

        private void OnEnable()
        {
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
            _levelManager.LevelLoaded += BindLevel;
            _levelManager.LevelUnloading += ReleaseLevel;
            _reader.LanguageCycled += CycleLanguage;
            _cameraRotation.FacingChanged += HandleFacingChanged;
        }

        private void OnDisable()
        {
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
            _levelManager.LevelLoaded -= BindLevel;
            _levelManager.LevelUnloading -= ReleaseLevel;
            _reader.LanguageCycled -= CycleLanguage;
            _cameraRotation.FacingChanged -= HandleFacingChanged;
        }

        // a metà di uno scatto della camera: i lati vicini diventano bassi, quelli lontani alti
        private void HandleFacingChanged(int facing)
        {
            if (_walls != null)
            {
                _walls.Show(facing);
            }
        }

        // tasto provvisorio (D13 della M5): la scelta vera andrà nel menu delle opzioni della M10
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
            _levelTitle.Show(tileset != null ? tileset.NameKey : null, level.Depth);
            _depth = level.Depth;

            foreach (var enemy in level.Enemies)
            {
                // un nemico può essere già morto e sparito se il livello era aperto da prima
                if (enemy == null)
                {
                    continue;
                }

                enemy.Bind(_player);
                enemy.Killed += HandleEnemyKilled;
                _rewardingEnemies.Add(enemy);
                if (enemy.TryGetComponent(out CharacterAudio audio))
                {
                    audio.Bind(_sfxLimiter);
                }
                var attack = enemy.GetComponent<MeleeAttack>();
                attack.SetRandomSource(_random);
                _enemyAttacks.Add(attack);
                var health = enemy.GetComponent<Health>();
                _damageNumbers.Track(health, isPlayer: false);
                _trackedEnemies.Add(health);
                if (enemy.TryGetComponent(out LootDrop loot))
                {
                    loot.Bind(_loot, level.Depth);
                }
            }

            foreach (var chest in level.Chests)
            {
                chest.Bind(_loot, level.Depth);
            }

            _pack = new EnemyPack(level.Enemies);

            // il cavaliere è già sull'ingresso: l'automappa parte scoprendo i suoi dintorni
            _exploration.SetLevel(level.Map, level.Depth);
        }

        private void ReleaseLevel(LevelContext level)
        {
            _walls = null;
            _pack?.Release();
            _pack = null;
            foreach (var health in _trackedEnemies)
            {
                _damageNumbers.Untrack(health);
            }

            _trackedEnemies.Clear();
            _enemyAttacks.Clear();

            // anche i nemici già distrutti: l'evento è un campo C#, staccarsi è sempre lecito
            foreach (var enemy in _rewardingEnemies)
            {
                if (!ReferenceEquals(enemy, null))
                {
                    enemy.Killed -= HandleEnemyKilled;
                }
            }

            _rewardingEnemies.Clear();
            _exploration.SetLevel(null, 0);
        }

        // l'esperienza del nemico ucciso, alla profondità del livello in cui è morto (D2 della M8)
        private void HandleEnemyKilled(EnemyAI enemy)
        {
            enemy.Killed -= HandleEnemyKilled;
            _rewardingEnemies.Remove(enemy);
            _progress.AwardKill(enemy.Archetype.Experience, _depth);
        }

        /// <summary>Cambia il seme della partita: per i test e per rigiocare un seme senza riavviare.</summary>
        public void UseLootSeed(ulong seed)
        {
            _loot.RunSeed = seed;
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
        }
    }
}
