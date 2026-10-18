using System;
using DarkDescent.Items;
using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quanto un campione o un unico supera un nemico normale dello stesso tipo (D2 e D6 della M12):
    /// moltiplicatori di vita, danno ed esperienza, la scala del modello e il colore di nome, tinta e
    /// anello, e il loot in più (D7).
    /// </summary>
    [Serializable]
    public sealed class RankNumbers
    {
        [SerializeField, Min(1f)] private float _life = 1f;

        [SerializeField, Min(1f)] private float _damage = 1f;

        [SerializeField, Min(1f)] private float _experience = 1f;

        [Tooltip("La scala del modello, del collider e del raggio dell'agent.")]
        [SerializeField, Min(1f)] private float _scale = 1f;

        [Tooltip("Il colore del nome, della tinta del modello e dell'anello sotto i piedi.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("Il loot in più: tiri, rarità spostate, rarità garantita e oro (D7 della M12).")]
        [SerializeField] private LootBonus _loot = new LootBonus();

        public RankNumbers()
        {
        }

        public RankNumbers(float life, float damage, float experience, float scale, Color color, LootBonus loot)
        {
            _life = life;
            _damage = damage;
            _experience = experience;
            _scale = scale;
            _color = color;
            _loot = loot ?? new LootBonus();
        }

        public float Life => _life;

        public float Damage => _damage;

        public float Experience => _experience;

        public float Scale => _scale;

        public Color Color => _color;

        public LootBonus Loot => _loot;
    }
}
