using System.Collections.Generic;
using DarkDescent.Rendering;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I muri e le torce di ogni lato, nella versione alta e in quella bassa (D12 della M8). Il
    /// builder li mette tutti; qui si accende, per ogni lato, la versione giusta per la camera: alta
    /// sui lati lontani, bassa su quelli vicini, che così non coprono il cavaliere (ADR-017). Il
    /// livello nasce con tutti i lati alti, ed è così che si cuoce il NavMesh.
    /// </summary>
    [DisallowMultipleComponent]
    public class WallView : MonoBehaviour
    {
        // tre liste parallele: il gruppo, il suo lato e se è la versione alta
        [SerializeField] private List<GameObject> _groups = new List<GameObject>();
        [SerializeField] private List<MapDirection> _sides = new List<MapDirection>();
        [SerializeField] private List<bool> _high = new List<bool>();

        /// <summary>Il lato da cui guarda la camera, l'ultimo mostrato; -1 prima del primo.</summary>
        public int Facing { get; private set; } = -1;

        /// <summary>Per il builder: un gruppo che sta acceso quando il suo lato è lontano (alto) o vicino (basso).</summary>
        public void Add(GameObject group, MapDirection side, bool high)
        {
            _groups.Add(group);
            _sides.Add(side);
            _high.Add(high);
        }

        /// <summary>Accende i gruppi giusti per la camera che guarda dal lato <paramref name="facing"/>.</summary>
        public void Show(int facing)
        {
            Facing = facing;
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].SetActive(ViewRotation.IsFar(_sides[i], facing) == _high[i]);
            }
        }
    }
}
