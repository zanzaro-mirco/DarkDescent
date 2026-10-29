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
        private NavMeshData _navMesh;

        /// <summary>Quanto è costato il livello, in millisecondi: generazione (o lettura della mappa), costruzione, NavMesh (D4 della M6).</summary>
        public (long generate, long build, long bake) Timings { get; protected set; }

        /// <summary>Costruisce il livello alla profondità chiesta, una volta sola; le chiamate dopo la prima restituiscono quello.</summary>
        public abstract LevelContext Build(ulong runSeed, int depth);

        /// <summary>
        /// Cuoce il NavMesh dai collider e accende i nemici sopra di lui. Dai collider: le mesh dei
        /// modelli non sono leggibili, e in build il NavMesh verrebbe vuoto (D5 della M6).
        /// </summary>
        protected void Bake(LevelContext context)
        {
            var surface = context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            _navMesh = surface.navMeshData;

            // i nemici si accendono sopra il NavMesh appena cotto: i loro agent ci si agganciano
            context.transform.Find(LevelBuilder.EnemiesGroup).gameObject.SetActive(true);
        }

        // il NavMesh cotto è un oggetto nuovo a ogni costruzione, e la scena che si scarica non lo
        // distrugge: ne restava uno per ogni livello visitato (partita lunga della M15)
        private void OnDestroy()
        {
            if (_navMesh != null)
            {
                Destroy(_navMesh);
            }
        }
    }
}
