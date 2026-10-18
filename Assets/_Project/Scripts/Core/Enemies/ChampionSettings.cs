using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// I numeri dei nemici speciali (M12): quanto sono più forti campioni e unici, e i modificatori
    /// che i campioni possono prendere. Quanti ce ne sono per livello sta nelle impostazioni del
    /// livello (<c>SpecialEnemyRules</c>). Dati immutabili.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Champion Settings", fileName = "ChampionSettings")]
    public sealed class ChampionSettings : ScriptableObject
    {
        [Header("Campioni (D2 della M12)")]
        [SerializeField] private RankNumbers _champion = new RankNumbers(3f, 1.5f, 4f, 1.15f, new Color(0.45f, 0.6f, 1f, 1f));

        [Header("Unici (D6 della M12)")]
        [SerializeField] private RankNumbers _unique = new RankNumbers(5f, 1.8f, 8f, 1.3f, new Color(0.9f, 0.75f, 0.35f, 1f));

        [Tooltip("I modificatori tra cui si sceglie per i campioni (D5 della M12); l'ordine conta per il seme.")]
        [SerializeField] private List<ChampionModifier> _modifiers = new List<ChampionModifier>();

        public IReadOnlyList<ChampionModifier> Modifiers => _modifiers;

        /// <summary>Gli id dei modificatori, nello stesso ordine: quello che serve al popolatore, una volta per livello.</summary>
        public List<string> CollectModifierIds()
        {
            var ids = new List<string>(_modifiers.Count);
            foreach (var modifier in _modifiers)
            {
                if (modifier != null)
                {
                    ids.Add(modifier.Id);
                }
            }

            return ids;
        }

        /// <summary>I numeri di un grado: null per i nemici normali, che restano come sono.</summary>
        public RankNumbers For(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Champion: return _champion;
                case EnemyRank.Unique: return _unique;
                default: return null;
            }
        }

        /// <summary>Il modificatore con questo id, o null se non c'è.</summary>
        public ChampionModifier Find(string id)
        {
            foreach (var modifier in _modifiers)
            {
                if (modifier != null && modifier.Id == id)
                {
                    return modifier;
                }
            }

            return null;
        }
    }
}
