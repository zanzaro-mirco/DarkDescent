using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Per il <see cref="LevelBuilder"/>: sotto un gruppo del livello, un figlio per lato con dentro
    /// la versione alta e quella bassa, registrate nella <see cref="WallView"/>. Le alte nascono
    /// accese e le basse spente: il NavMesh si cuoce con i muri alti su tutti i lati.
    /// </summary>
    internal sealed class SideGroups
    {
        private readonly Transform[] _high = new Transform[4];
        private readonly Transform[] _low = new Transform[4];

        public SideGroups(Transform parent, WallView view)
        {
            for (int i = 0; i < 4; i++)
            {
                var side = (MapDirection)i;
                var group = new GameObject(side.ToString()).transform;
                group.SetParent(parent, false);
                _high[i] = Create(group, "High", side, true, view);
                _low[i] = Create(group, "Low", side, false, view);
            }
        }

        public Transform High(MapDirection side)
        {
            return _high[(int)side];
        }

        public Transform Low(MapDirection side)
        {
            return _low[(int)side];
        }

        private static Transform Create(Transform parent, string name, MapDirection side, bool high, WallView view)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.SetActive(high);
            view.Add(go, side, high);
            return go.transform;
        }
    }
}
