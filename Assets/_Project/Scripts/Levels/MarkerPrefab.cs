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

        [Tooltip("Il prefab ha già il suo verso (un tratto di recinto da est a ovest): non si gira verso la stanza.")]
        [SerializeField] private bool _keepsRotation;

        public char Symbol => _symbol;

        public GameObject Prefab => _prefab;

        public bool ReplacesFloor => _replacesFloor;

        public bool KeepsRotation => _keepsRotation;
    }
}
