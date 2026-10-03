using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>Un carattere della mappa e l'oggetto che lo rappresenta al centro della sua cella.</summary>
    [Serializable]
    public struct MarkerPrefab
    {
        [SerializeField] private char _symbol;
        [SerializeField] private GameObject _prefab;

        public char Symbol => _symbol;

        public GameObject Prefab => _prefab;
    }
}
