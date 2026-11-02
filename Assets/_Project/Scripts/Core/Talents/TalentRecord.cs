using System;
using UnityEngine;

namespace DarkDescent.Talents
{
    /// <summary>Un nodo preso, nel salvataggio (formato 8, M20): l'albero, il nodo, i gradi e l'opzione scelta.</summary>
    [Serializable]
    public struct TalentRecord
    {
        [SerializeField] private string _tree;
        [SerializeField] private string _node;
        [SerializeField] private int _rank;
        [SerializeField] private int _option;

        public TalentRecord(string tree, string node, int rank, int option)
        {
            _tree = tree;
            _node = node;
            _rank = rank;
            _option = option;
        }

        public string Tree => _tree;
        public string Node => _node;
        public int Rank => _rank;
        public int Option => _option;
    }
}
