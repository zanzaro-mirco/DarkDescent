using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Quanti nemici speciali in un livello e dove (D3, D4 e D6 della M12), nelle impostazioni del
    /// livello: la cripta e le caverne hanno numeri loro. I numeri si ritarano qui, provando la build.
    /// </summary>
    [Serializable]
    public sealed class SpecialEnemyRules
    {
        [Tooltip("La prima profondità con campioni e con l'unico: alla prima si impara a giocare.")]
        [SerializeField, Min(1)] private int _minDepth = 2;

        [Tooltip("Gruppi di campioni sicuri in ogni livello: uno nella cripta, due nelle caverne.")]
        [SerializeField, Min(0)] private int _groups = 1;

        [Tooltip("Probabilità di un gruppo in più, tra 0 e 1.")]
        [SerializeField, Range(0f, 1f)] private float _extraGroupChance = 0.4f;

        [Tooltip("Quanti nemici normali in più fanno da scorta a un campione o all'unico, da e a.")]
        [SerializeField] private Vector2Int _escort = new Vector2Int(2, 4);

        [Tooltip("La scorta di un campione dello sciame, che caccia in branco.")]
        [SerializeField] private Vector2Int _swarmEscort = new Vector2Int(4, 6);

        [Tooltip("La scorta sta entro tante celle dal suo capo.")]
        [SerializeField, Min(1)] private int _escortRadius = 2;

        [Tooltip("Due gruppi speciali stanno almeno a tante celle l'uno dall'altro.")]
        [SerializeField, Min(1)] private int _spacing = 4;

        [Tooltip("Nessun nemico speciale entro tante celle dall'ingresso del livello.")]
        [SerializeField, Min(1)] private int _entranceDistance = 4;

        [Tooltip("Da questa profondità i campioni hanno due modificatori invece di uno.")]
        [SerializeField, Min(1)] private int _twoModifiersFrom = 5;

        [Tooltip("La probabilità, a ogni profondità, che l'unico del livello sia lì; una volta trovato, nelle profondità dopo non c'è più (D6).")]
        [SerializeField, Range(0f, 1f)] private float _uniqueChance = 1f / 3f;

        [Tooltip("Il simbolo dei nemici della scorta dell'unico: scheletri.")]
        [SerializeField] private string _uniqueEscort = "S";

        [Tooltip("La probabilità che un campione abbia un forziere accanto; l'unico ce l'ha sempre (D8).")]
        [SerializeField, Range(0f, 1f)] private float _goldChestChance = 0.5f;

        public SpecialEnemyRules()
        {
        }

        public SpecialEnemyRules(int minDepth, int groups, float extraGroupChance, Vector2Int escort, Vector2Int swarmEscort, int twoModifiersFrom, float uniqueChance)
        {
            _minDepth = minDepth;
            _groups = groups;
            _extraGroupChance = extraGroupChance;
            _escort = escort;
            _swarmEscort = swarmEscort;
            _twoModifiersFrom = twoModifiersFrom;
            _uniqueChance = uniqueChance;
        }

        public int MinDepth => _minDepth;

        public int Groups => _groups;

        public float ExtraGroupChance => _extraGroupChance;

        public Vector2Int Escort => _escort;

        public Vector2Int SwarmEscort => _swarmEscort;

        public int EscortRadius => _escortRadius;

        public int Spacing => _spacing;

        public int EntranceDistance => _entranceDistance;

        public float UniqueChance => _uniqueChance;

        public float GoldChestChance => _goldChestChance;

        public char UniqueEscort => string.IsNullOrEmpty(_uniqueEscort) ? DungeonPopulator.EnemySymbol : _uniqueEscort[0];

        /// <summary>Quanti modificatori ha un campione a questa profondità.</summary>
        public int ModifiersAt(int depth)
        {
            return depth >= _twoModifiersFrom ? 2 : 1;
        }
    }
}
