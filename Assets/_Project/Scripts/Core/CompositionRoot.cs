using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Player;
using DarkDescent.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [SerializeField] private ItemCursor _itemCursor;

        [SerializeField] private HitStop _hitStop;

        [SerializeField] private CinemachineCamera _playerCamera;

        [Tooltip("La tabella delle stringhe: una colonna per lingua (D12 della M5).")]
        [SerializeField] private TextAsset _strings;

        [Tooltip("Gli affissi che il generatore può tirare (D3 della M5).")]
        [SerializeField] private AffixDatabase _affixes;

        /// <summary>Dove resta la lingua scelta tra un avvio e l'altro: è una preferenza, non la partita.</summary>
        public const string LanguagePreference = "language";

        private const string LanguageOption = "-lang";

        private const string SeedOption = "-seed";

        private MeleeAttack _playerAttack;
        private PlayerInputReader _reader;
        private Localizer _localizer;
        private LootRoller _loot;

        // un generatore solo per tutti i tiri del combattimento, con un seme diverso a ogni avvio (D3)
        private IRandomSource _random;

        private readonly List<MeleeAttack> _enemyAttacks = new List<MeleeAttack>();

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
            if (!CommandLine.TryGetValue(Environment.GetCommandLineArgs(), SeedOption, out string seedText)
                || !ulong.TryParse(seedText, out ulong seed))
            {
                seed = (ulong)DateTime.UtcNow.Ticks;
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
            _characterPanel.Bind(_player.GetComponent<Stats.CharacterStats>(), inventory, _reader);
        }

        private void OnEnable()
        {
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
            _levelManager.LevelLoaded += BindLevel;
            _levelManager.LevelUnloading += ReleaseLevel;
            _reader.LanguageCycled += CycleLanguage;
        }

        private void OnDisable()
        {
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
            _levelManager.LevelLoaded -= BindLevel;
            _levelManager.LevelUnloading -= ReleaseLevel;
            _reader.LanguageCycled -= CycleLanguage;
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

            foreach (var enemy in level.Enemies)
            {
                // un nemico può essere già morto e sparito se il livello era aperto da prima
                if (enemy == null)
                {
                    continue;
                }

                enemy.Bind(_player);
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
        }

        private void ReleaseLevel(LevelContext level)
        {
            foreach (var health in _trackedEnemies)
            {
                _damageNumbers.Untrack(health);
            }

            _trackedEnemies.Clear();
            _enemyAttacks.Clear();
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
            _hitStop.Trigger();
        }

        private void Restart()
        {
            // timeScale sopravvive al caricamento della scena (trappola 6 della M2): un hit stop o una
            // pausa rimasti a metà farebbero ripartire il gioco rallentato o fermo
            Time.timeScale = 1f;

            // Core in modalità singola scarica anche il livello; il LevelManager riparte dal primo (D8)
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
