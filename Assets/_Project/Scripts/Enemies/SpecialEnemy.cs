using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Un nemico speciale (M12): campione o unico, con i suoi modificatori. Gli unici lo hanno nel
    /// prefab, con modificatori e tinta fissi (D6); un nemico normale lo riceve dal builder quando la
    /// mappa lo fa campione (direttiva <c>@champions</c>). I modificatori sono id: li risolve il
    /// composition root con le <see cref="ChampionSettings"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpecialEnemy : MonoBehaviour
    {
        [SerializeField] private EnemyRank _rank = EnemyRank.Champion;

        [Tooltip("Gli id dei modificatori: fissi per gli unici, scelti dal seme per i campioni.")]
        [SerializeField] private string[] _modifiers = Array.Empty<string>();

        [Tooltip("La tinta del modello degli unici; i campioni prendono il colore del loro grado.")]
        [SerializeField] private Color _tint = Color.white;

        public EnemyRank Rank => _rank;

        public IReadOnlyList<string> Modifiers => _modifiers;

        public Color Tint => _tint;

        /// <summary>Lo fa campione con questi modificatori: lo chiama il builder, prima che il nemico si svegli.</summary>
        public void Promote(EnemyRank rank, IReadOnlyList<string> modifiers)
        {
            _rank = rank;
            _modifiers = new string[modifiers != null ? modifiers.Count : 0];
            for (int i = 0; i < _modifiers.Length; i++)
            {
                _modifiers[i] = modifiers[i];
            }
        }
    }
}
