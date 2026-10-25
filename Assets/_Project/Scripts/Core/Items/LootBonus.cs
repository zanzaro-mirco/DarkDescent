using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il loot in più di chi vale di più (D7 e D8 della M12): campioni, unici, forzieri e il tesoro
    /// del re. Tiri in più della stessa tabella, ognuno con un seme suo, rarità spostate verso l'alto,
    /// una rarità garantita su tutto quello che cade e l'oro moltiplicato. Il primo tiro resta quello
    /// di sempre: un nemico promosso lascia lo stesso oggetto che lasciava da normale, più il resto.
    /// Dalla M13 (D13) campioni e unici non hanno il tiro di sempre: lasciano solo i tiri in più.
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

        [Tooltip("Senza il tiro di sempre: cadono solo i tiri in più (campioni, unici e forzieri, D13 della M13).")]
        [SerializeField] private bool _replacesRoll;

        [Tooltip("La probabilità che cada ogni tiro in più: 1 sempre, 0,5 il campione (D13 della M13).")]
        [SerializeField, Range(0f, 1f)] private float _extraChance = 1f;

        [Tooltip("Per quanto si moltiplica la probabilità di unici e set dei tiri in più: 3 campioni, unici e forzieri, 10 il tesoro del re (D7 della M16).")]
        [SerializeField, Min(1f)] private float _namedFactor = 1f;

        [Tooltip("La probabilità di un tiro a parte che fa unico o pezzo di set un oggetto dei tiri in più, se nessuno lo è già: il tesoro del re (D7 della M16).")]
        [SerializeField, Range(0f, 1f)] private float _namedGuarantee;

        public LootBonus()
        {
        }

        public LootBonus(int extraRolls, float magicFactor, float rareFactor, Rarity guaranteed, float goldFactor, bool replacesRoll = false,
            float extraChance = 1f, float namedFactor = 1f, float namedGuarantee = 0f)
        {
            _namedFactor = namedFactor;
            _namedGuarantee = namedGuarantee;
            _replacesRoll = replacesRoll;
            _extraChance = extraChance;
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

        public bool ReplacesRoll => _replacesRoll;

        public float ExtraChance => _extraChance;

        public float NamedFactor => _namedFactor;

        public float NamedGuarantee => _namedGuarantee;
    }
}
