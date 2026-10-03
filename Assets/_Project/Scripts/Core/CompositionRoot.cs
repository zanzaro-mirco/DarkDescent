using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Levels;
using DarkDescent.UI;
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

        [SerializeField] private HitStop _hitStop;

        private MeleeAttack _playerAttack;

        // le Health dei nemici seguite dai numeri di danno: al cambio di livello vanno lasciate, anche
        // quelle già distrutte, di cui non si potrebbe più chiedere il componente all'EnemyAI
        private readonly List<Health> _trackedEnemies = new List<Health>();

        private void Awake()
        {
            _playerAttack = _player.GetComponent<MeleeAttack>();

            _healthOrb.Bind(_player);
            _deathScreen.Bind(_player);
            _damageNumbers.Track(_player, isPlayer: true);
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
            foreach (var enemy in level.Enemies)
            {
                // un nemico può essere già morto e sparito se il livello era aperto da prima
                if (enemy == null)
                {
                    continue;
                }

                enemy.Bind(_player);
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
