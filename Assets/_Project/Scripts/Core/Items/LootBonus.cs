using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il loot in più di chi vale di più (D7 e D8 della M12): campioni, unici, forzieri e il tesoro
    /// del re. Tiri in più della stessa tabella, ognuno con un seme suo, rarità spostate verso l'alto,
    /// una rarità garantita su tutto quello che cade e l'oro moltiplicato. Il primo tiro resta quello
    /// di sempre: un nemico promosso lascia lo stesso oggetto che lasciava da normale, più il resto.
    /// </summary>
    [Serializable]
    public sealed class LootBonus
    {
        [Tooltip("Quanti oggetti in più del primo: cadono sempre, senza il tiro della probabilità.")]
        [SerializeField, Min(0)] private int _extraRolls;

        [Tooltip("Per quanto si moltiplica il peso dei magici nei tiri in più.")]
        [SerializeField, Min(0f)] private float _magicFactor = 1f;

        [Tooltip("Per quanto si moltiplica il peso dei rari nei tiri in più.")]
        [SerializeField, Min(0f)] private float _rareFactor = 1f;

        [Tooltip("Almeno un oggetto di questa rarità tra tutti quelli che cadono: se manca, lo diventa l'ultimo tiro in più.")]
        [SerializeField] private Rarity _guaranteed = Rarity.Normal;

        [Tooltip("Per quanto si moltiplica l'oro, quando cade.")]
        [SerializeField, Min(1f)] private float _goldFactor = 1f;

        public LootBonus()
        {
        }

        public LootBonus(int extraRolls, float magicFactor, float rareFactor, Rarity guaranteed, float goldFactor)
        {
            _extraRolls = extraRolls;
            _magicFactor = magicFactor;
            _rareFactor = rareFactor;
            _guaranteed = guaranteed;
            _goldFactor = goldFactor;
        }

        public int ExtraRolls => _extraRolls;

        public float MagicFactor => _magicFactor;

        public float RareFactor => _rareFactor;

        public Rarity Guaranteed => _guaranteed;

        public float GoldFactor => _goldFactor;
    }
}
