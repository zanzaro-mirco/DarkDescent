using DarkDescent.Combat;
using DarkDescent.Enemies;
using UnityEngine;

namespace DarkDescent.Core
{
    /// <summary>
    /// Collega i sistemi della scena in un posto solo, leggibile dall'alto in basso (D1 della scheda M2).
    /// Al passo 2.5 dice ai nemici chi è il player; al 2.6 collega anche l'HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompositionRoot : MonoBehaviour
    {
        [SerializeField] private Health _player;

        private void Awake()
        {
            // ricerca una volta sola, all'avvio della scena; i nemici generati più avanti li collegherà lo spawner
            foreach (var enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.Bind(_player);
            }
        }
    }
}
