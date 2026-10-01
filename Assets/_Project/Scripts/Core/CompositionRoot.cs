using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkDescent.Core
{
    /// <summary>
    /// Collega i sistemi della scena in un posto solo, leggibile dall'alto in basso (D1 della scheda M2).
    /// Dice ai nemici chi è il player, collega l'HUD, i numeri di danno e l'hit stop, e ricarica la
    /// scena per ricominciare.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompositionRoot : MonoBehaviour
    {
        [SerializeField] private Health _player;

        [SerializeField] private HealthOrb _healthOrb;

        [SerializeField] private DeathScreen _deathScreen;

        [SerializeField] private DamageNumbers _damageNumbers;

        [SerializeField] private HitStop _hitStop;

        private MeleeAttack _playerAttack;

        private void Awake()
        {
            _playerAttack = _player.GetComponent<MeleeAttack>();

            _healthOrb.Bind(_player);
            _deathScreen.Bind(_player);
            _damageNumbers.Track(_player, isPlayer: true);

            // ricerca una volta sola, all'avvio della scena; i nemici generati più avanti li collegherà lo spawner
            foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.Bind(_player);
                _damageNumbers.Track(enemy.GetComponent<Health>(), isPlayer: false);
            }
        }

        private void OnEnable()
        {
            _deathScreen.RestartRequested += Restart;
            _playerAttack.HitLanded += HandlePlayerHitLanded;
        }

        private void OnDisable()
        {
            _deathScreen.RestartRequested -= Restart;
            _playerAttack.HitLanded -= HandlePlayerHitLanded;
        }

        // solo i colpi del player fermano il tempo: con tre scheletri che colpiscono, il gioco singhiozzerebbe
        private void HandlePlayerHitLanded(DamageInfo info)
        {
            _hitStop.Trigger();
        }

        private void Restart()
        {
            // timeScale sopravvive al caricamento della scena (trappola 6): un hit stop o una pausa
            // rimasti a metà farebbero ripartire il gioco rallentato o fermo
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
