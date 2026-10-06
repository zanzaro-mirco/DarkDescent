using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I nemici di una profondità (D10 della M7): per ogni tipo, da quanti a quanti gruppi. Lo
    /// scheletro e lo sciame arrivano a gruppi, il bruto da solo, quindi per lui gruppi e nemici
    /// coincidono.
    /// </summary>
    [Serializable]
    public struct SpawnRow
    {
        [Tooltip("Da questa profondità in giù, finché non c'è una riga più profonda.")]
        [SerializeField, Min(1)] private int _depth;

        [Tooltip("Gruppi di scheletri: da quanti (x) a quanti (y).")]
        [SerializeField] private Vector2Int _skeletonGroups;

        [Tooltip("Gruppi di sciame: da quanti (x) a quanti (y).")]
        [SerializeField] private Vector2Int _swarmGroups;

        [Tooltip("Bruti: da quanti (x) a quanti (y).")]
        [SerializeField] private Vector2Int _brutes;

        public SpawnRow(int depth, Vector2Int skeletonGroups, Vector2Int swarmGroups, Vector2Int brutes)
        {
            _depth = depth;
            _skeletonGroups = skeletonGroups;
            _swarmGroups = swarmGroups;
            _brutes = brutes;
        }

        public int Depth => _depth;

        public Vector2Int SkeletonGroups => _skeletonGroups;

        public Vector2Int SwarmGroups => _swarmGroups;

        public Vector2Int Brutes => _brutes;
    }
}
