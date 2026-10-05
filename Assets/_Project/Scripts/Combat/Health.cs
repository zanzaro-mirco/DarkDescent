using System;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Guscio Unity di HealthModel: lo collega alla scena e pubblica gli eventi
    /// a cui si iscrivono HUD, flash, numeri di danno, suoni e IA.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("Vita massima. Ignorata se CharacterStats la ricava dalla Vitalità, come per il cavaliere.")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        private HealthModel _model;
        private CharacterStats _stats;

        /// <summary>Vita corrente e massima, dopo ogni danno applicato.</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Il colpo ricevuto e il danno effettivamente applicato.</summary>
        public event Action<DamageInfo, float> Damaged;

        /// <summary>Emesso una sola volta.</summary>
        public event Action Died;

        /// <summary>La vita tornata con una cura, dopo HealthChanged.</summary>
        public event Action<float> Healed;

        /// <summary>Un colpo mancato: nessun danno, nessun lampo, solo la scritta.</summary>
        public event Action<DamageInfo> Evaded;

        public float Current => Model.Current;
        public float Max => Model.Max;
        public bool IsDead => Model.IsDead;

        // Creato al primo accesso, non in Awake: chi si collega in Awake da un altro oggetto
        // (il composition root, la sfera della vita) può arrivare prima, perché l'ordine
        // degli Awake tra oggetti diversi non è garantito.
        private HealthModel Model => _model ??= new HealthModel(MaxLife());

        public void TakeDamage(in DamageInfo info)
        {
            // Gli eventi si inoltrano da qui e non iscrivendosi a quelli del model:
            // nessun += da bilanciare, e l'ordine (vita, colpo, morte) è deciso in un punto solo.
            bool wasDead = Model.IsDead;
            float applied = Model.ApplyDamage(info.Amount);
            if (applied <= 0f)
            {
                return;
            }

            HealthChanged?.Invoke(Model.Current, Model.Max);
            Damaged?.Invoke(info, applied);

            if (!wasDead && Model.IsDead)
            {
                Died?.Invoke();
            }
        }

        /// <summary>Rende vita fino al massimo; restituisce quanta ne è tornata, 0 da morto o a vita piena.</summary>
        public float Heal(float amount)
        {
            float applied = Model.Heal(amount);
            if (applied > 0f)
            {
                HealthChanged?.Invoke(Model.Current, Model.Max);
                Healed?.Invoke(applied);
            }

            return applied;
        }

        public void Evade(in DamageInfo info)
        {
            if (!Model.IsDead)
            {
                Evaded?.Invoke(info);
            }
        }

        private float MaxLife()
        {
            return TryGetComponent(out CharacterStats stats) && stats.LifeFromVitality
                ? CombatFormulas.MaxLife(stats.Vitality) + stats.Life
                : _maxHealth;
        }

        private void Awake()
        {
            TryGetComponent(out _stats);
        }

        private void OnEnable()
        {
            if (_stats != null && _stats.LifeFromVitality)
            {
                _stats.Sheet.Changed += HandleStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (_stats != null && _stats.LifeFromVitality)
            {
                _stats.Sheet.Changed -= HandleStatsChanged;
            }
        }

        // un oggetto della Vitalità o della vita: il massimo cambia con la regola di D10
        private void HandleStatsChanged()
        {
            float before = Model.Max;
            Model.SetMax(MaxLife());
            if (Model.Max != before)
            {
                HealthChanged?.Invoke(Model.Current, Model.Max);
            }
        }
    }
}
