using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.UI;
using UnityEngine;

namespace DarkDescent.Core
{
    /// <summary>
    /// Collega i sistemi della scena in un posto solo, leggibile dall'alto in basso (D1 della scheda M2).
    /// Dice ai nemici chi è il player e collega l'HUD alla sua vita.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompositionRoot : MonoBehaviour
    {
        [SerializeField] private Health _player;

        [SerializeField] private HealthOrb _healthOrb;

        private void Awake()
        {
            _healthOrb.Bind(_player);

            // ricerca una volta sola, all'avvio della scena; i nemici generati più avanti li collegherà lo spawner
            foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.Bind(_player);
            }
        }
    }
}
