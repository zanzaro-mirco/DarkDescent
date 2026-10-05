using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Punto in cui il player compare entrando in un livello. Le uscite degli altri livelli lo
    /// indicano per id: la scala che scende arriva all'ingresso "FromAbove" del livello sotto.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelEntrance : MonoBehaviour
    {
        [SerializeField] private string _id = "Start";

        public string Id => _id;

        /// <summary>Per chi costruisce il livello (<see cref="LevelBuilder"/>).</summary>
        public void Configure(string id)
        {
            _id = id;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}
