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

        [Tooltip("Il prefab ha già il suo pavimento (la piastrella degli spuntoni): quello di sempre non si mette.")]
        [SerializeField] private bool _replacesFloor;

        public char Symbol => _symbol;

        public GameObject Prefab => _prefab;

        public bool ReplacesFloor => _replacesFloor;
    }
}
