using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
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

        private MeleeAttack _playerAttack;

        // un generatore solo per tutti i tiri del combattimento, con un seme diverso a ogni avvio (D3)
        private IRandomSource _random;

        private readonly List<MeleeAttack> _enemyAttacks = new List<MeleeAttack>();

        // le Health dei nemici seguite dai numeri di danno: al cambio di livello vanno lasciate, anche
        // quelle già distrutte, di cui non si potrebbe più chiedere il componente all'EnemyAI
        private readonly List<Health> _trackedEnemies = new List<Health>();

        private void Awake()
        {
            _playerAttack = _player.GetComponent<MeleeAttack>();
            _random = new SystemRandomSource(Environment.TickCount);
            _playerAttack.SetRandomSource(_random);

            _healthOrb.Bind(_player);
            _deathScreen.Bind(_player);
            _damageNumbers.Track(_player, isPlayer: true);
            _interactableLabel.Bind(_player.GetComponent<PlayerController>());

            var inventory = _player.GetComponent<PlayerInventory>();
            var reader = _player.GetComponent<PlayerInputReader>();
            _inventoryPanel.Bind(inventory, reader);
            _itemCursor.Bind(inventory, reader);
            _characterPanel.Bind(_player.GetComponent<Stats.CharacterStats>(), inventory, reader);
        }

        private void OnEnable()
        {
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
            _levelManager.LevelLoaded += BindLevel;
            _levelManager.LevelUnloading += ReleaseLevel;
        }

        private void OnDisable()
        {
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
            _levelManager.LevelLoaded -= BindLevel;
            _levelManager.LevelUnloading -= ReleaseLevel;
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
