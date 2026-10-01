using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkDescent.Core
{
    /// <summary>
    /// Collega i sistemi della scena in un posto solo, leggibile dall'alto in basso (D1 della scheda M2).
    /// Dice ai nemici chi è il player, collega l'HUD alla sua vita e ricarica la scena per ricominciare.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompositionRoot : MonoBehaviour
    {
        [SerializeField] private Health _player;

        [SerializeField] private HealthOrb _healthOrb;

        [SerializeField] private DeathScreen _deathScreen;

        private void Awake()
        {
            _healthOrb.Bind(_player);
            _deathScreen.Bind(_player);

            // ricerca una volta sola, all'avvio della scena; i nemici generati più avanti li collegherà lo spawner
            foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.Bind(_player);
            }
        }

        private void OnEnable()
        {
            _deathScreen.RestartRequested += Restart;
        }

        private void OnDisable()
        {
            _deathScreen.RestartRequested -= Restart;
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
