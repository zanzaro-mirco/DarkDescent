using System;
using DarkDescent.Core;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Il blocco con lo scudo (D1 della M5): un colpo già andato a segno viene fermato con
    /// probabilità blocco dello scudo + Destrezza / 2, al massimo 75%. Bloccato, non fa danno e
    /// interrompe il colpo che il personaggio stava dando, come in Diablo 1. Senza scudo non blocca
    /// niente. Tira con la sorgente di chi attacca, quella del combattimento.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(CharacterStats))]
    public class ShieldBlock : MonoBehaviour
    {
        [Tooltip("Secondi in cui il personaggio resta fermo dopo un blocco: vicino alla durata della clip.")]
        [SerializeField, Min(0f)] private float _recovery = 0.45f;

        private Health _health;
        private CharacterStats _stats;
        private MeleeAttack _attack;
        private bool _hasShield;
        private int _shieldBlock;

        /// <summary>Un colpo è stato bloccato: animazione, suono, scritta.</summary>
        public event Action<DamageInfo> Blocked;

        /// <summary>Lo scudo in mano è cambiato: chi mostra la probabilità di blocco la rilegge.</summary>
        public event Action ShieldChanged;

        public bool HasShield => _hasShield;

        /// <summary>La probabilità attuale, in punti percentuali; 0 senza scudo.</summary>
        public float BlockChance => _hasShield ? CombatFormulas.BlockChance(_shieldBlock, _stats.Dexterity) : 0f;

        /// <summary>Lo scudo in mano e il suo blocco; senza scudo, false. Lo chiama l'inventario.</summary>
        public void SetShield(bool hasShield, int shieldBlock)
        {
            _hasShield = hasShield;
            _shieldBlock = hasShield ? shieldBlock : 0;
            ShieldChanged?.Invoke();
        }

        /// <summary>Tira il blocco per un colpo andato a segno; true se l'ha fermato.</summary>
        public bool TryBlock(in DamageInfo info, IRandomSource random)
        {
            if (!_hasShield || _health.IsDead || !CombatFormulas.RollBlock(BlockChance, random))
            {
                return false;
            }

            if (_attack != null)
            {
                _attack.Interrupt(_recovery);
            }

            Blocked?.Invoke(info);
            return true;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _stats = GetComponent<CharacterStats>();
            _attack = GetComponent<MeleeAttack>();
        }
    }
}
