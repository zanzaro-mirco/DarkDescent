using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// I gradini della difficoltà, dal Crepuscolo all'ultimo Abisso (D2 della M14): una riga per
    /// gradino, in ordine. Un gradino in più è una riga in più, senza codice. Dati immutabili.
    /// </summary>
    [CreateAssetMenu(fileName = "Difficulties", menuName = "DarkDescent/Difficulties")]
    public sealed class DifficultyTable : ScriptableObject
    {
        [Tooltip("In ordine: il primo è quello di una partita nuova, ognuno apre il successivo.")]
        [SerializeField] private List<DifficultyTier> _tiers = new List<DifficultyTier>();

        public int Count => _tiers.Count;

        /// <summary>Il gradino numero <paramref name="index"/>; fuori misura, il più vicino.</summary>
        public DifficultyTier Tier(int index)
        {
            return _tiers[Mathf.Clamp(index, 0, _tiers.Count - 1)];
        }
    }
}
