using System;
using System.Collections.Generic;
using DarkDescent.Core;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Dove cadono le pergamene (D5 della M9), immutabile: un tiro per livello, dalla profondità
    /// <see cref="FirstDepth"/>, e la pergamena la porta uno dei nemici o delle casse del livello.
    /// Una pergamena di un incantesimo che il cavaliere conosce già non cade più.
    /// </summary>
    [CreateAssetMenu(fileName = "ScrollTable", menuName = "DarkDescent/Items/Scroll Table")]
    public sealed class ScrollTable : ScriptableObject
    {
        [SerializeField] private List<ScrollDefinition> _scrolls = new List<ScrollDefinition>();

        [Tooltip("La prima profondità in cui può cadere una pergamena.")]
        [SerializeField, Min(1)] private int _firstDepth = 2;

        [Tooltip("Probabilità che un livello abbia una pergamena: 0,34 è circa una ogni tre livelli.")]
        [SerializeField, Range(0f, 1f)] private float _chancePerLevel = 0.34f;

        public IReadOnlyList<ScrollDefinition> Scrolls => _scrolls;

        public int FirstDepth => _firstDepth;

        public float ChancePerLevel => _chancePerLevel;

        /// <summary>
        /// Tira la pergamena di un livello: false se non ce n'è. <paramref name="wanted"/> dice quali
        /// incantesimi può ancora insegnare; <paramref name="carrier"/>, tra 0 e 1, sceglie chi la porta.
        /// </summary>
        public bool TryRoll(IRandomSource random, int depth, Func<SpellDefinition, bool> wanted, out ScrollDefinition scroll, out double carrier)
        {
            scroll = null;
            carrier = 0.0;
            if (depth < _firstDepth || random.NextDouble() >= _chancePerLevel)
            {
                return false;
            }

            int count = 0;
            foreach (var candidate in _scrolls)
            {
                if (IsWanted(candidate, wanted))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            int pick = Math.Min(count - 1, (int)(random.NextDouble() * count));
            foreach (var candidate in _scrolls)
            {
                if (IsWanted(candidate, wanted) && pick-- == 0)
                {
                    scroll = candidate;
                    break;
                }
            }

            carrier = random.NextDouble();
            return true;
        }

        private static bool IsWanted(ScrollDefinition scroll, Func<SpellDefinition, bool> wanted)
        {
            return scroll != null && scroll.Spell != null && (wanted == null || wanted(scroll.Spell));
        }
    }
}
