using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La scena di un livello che si costruisce quando il LevelManager ci entra: la cripta e le caverne
    /// generate (M6, M7) e la città fatta a mano (D3 della M10). Il LevelManager la cerca alla radice
    /// della scena se non c'è già un <see cref="LevelContext"/>.
    /// </summary>
    public abstract class BuiltLevel : MonoBehaviour
    {
        /// <summary>Costruisce il livello alla profondità chiesta, una volta sola; le chiamate dopo la prima restituiscono quello.</summary>
        public abstract LevelContext Build(ulong runSeed, int depth);

        /// <summary>
        /// Cuoce il NavMesh dai collider e accende i nemici sopra di lui. Dai collider: le mesh dei
        /// modelli non sono leggibili, e in build il NavMesh verrebbe vuoto (D5 della M6).
        /// </summary>
        protected static void Bake(LevelContext context)
        {
            var surface = context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            // i nemici si accendono sopra il NavMesh appena cotto: i loro agent ci si agganciano
            context.transform.Find(LevelBuilder.EnemiesGroup).gameObject.SetActive(true);
        }
    }
}
