using System.Collections.Generic;
using DarkDescent.Items;
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
        [SerializeField] private RankNumbers _champion = new RankNumbers(3f, 1.5f, 4f, 1.15f, new Color(0.45f, 0.6f, 1f, 1f),
            new LootBonus(1, 2f, 3f, Rarity.Normal, 2.5f));

        [Header("Unici (D6 della M12)")]
        [SerializeField] private RankNumbers _unique = new RankNumbers(5f, 1.8f, 8f, 1.3f, new Color(0.9f, 0.75f, 0.35f, 1f),
            new LootBonus(2, 2f, 3f, Rarity.Rare, 4f));

        [Tooltip("I modificatori tra cui si sceglie per i campioni (D5 della M12); l'ordine conta per il seme.")]
        [SerializeField] private List<ChampionModifier> _modifiers = new List<ChampionModifier>();

        [Header("Si vedono da lontano (D9 della M12)")]
        [Tooltip("L'anello luminoso sotto i piedi, tinto del colore del grado.")]
        [SerializeField] private GameObject _ring;

        [Tooltip("Quanto sopra il pavimento sta l'anello, per non sfarfallare con lui.")]
        [SerializeField, Min(0f)] private float _ringHeight = 0.04f;

        [Tooltip("Quanto il modello di un campione prende il colore del grado: 0 niente, 1 tutto.")]
        [SerializeField, Range(0f, 1f)] private float _championTint = 0.5f;

        [Header("I modificatori si vedono e si sentono (v1.1.1)")]
        [Tooltip("Rapido: la polvere che alza correndo, figlia del nemico.")]
        [SerializeField] private GameObject _dustTrail;

        [Tooltip("Gelido: l'aura di brina attorno al nemico.")]
        [SerializeField] private GameObject _frostAura;

        [Tooltip("Rigenerante: il bagliore verde a ogni secondo di vita ripresa.")]
        [SerializeField] private GameObject _regenBurst;

        [Tooltip("Inamovibile: le scintille quando lo si colpisce.")]
        [SerializeField] private GameObject _sparks;

        [Tooltip("Inamovibile: il suono di metallo quando lo si colpisce.")]
        [SerializeField] private AudioClip _clangClip;

        [Tooltip("Furioso: l'aura rossa che resta da quando si accende.")]
        [SerializeField] private GameObject _rageAura;

        [Tooltip("Furioso: il ruggito quando si accende.")]
        [SerializeField] private AudioClip _rageClip;

        [Tooltip("Furioso: quanto cresce quando si accende.")]
        [SerializeField, Min(1f)] private float _rageScale = 1.1f;

        [Tooltip("Furioso: la tinta del modello quando si accende, al posto di quella del grado.")]
        [SerializeField] private Color _rageTint = new Color(1f, 0.4f, 0.32f, 1f);

        public IReadOnlyList<ChampionModifier> Modifiers => _modifiers;

        public GameObject DustTrail => _dustTrail;

        public GameObject FrostAura => _frostAura;

        public GameObject RegenBurst => _regenBurst;

        public GameObject Sparks => _sparks;

        public AudioClip ClangClip => _clangClip;

        public GameObject RageAura => _rageAura;

        public AudioClip RageClip => _rageClip;

        public float RageScale => _rageScale;

        public Color RageTint => _rageTint;

        public GameObject Ring => _ring;

        public float RingHeight => _ringHeight;

        public float ChampionTint => _championTint;

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
