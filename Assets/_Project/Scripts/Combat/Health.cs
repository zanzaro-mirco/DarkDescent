using System;
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
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        private HealthModel _model;

        /// <summary>Vita corrente e massima, dopo ogni danno applicato.</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Il colpo ricevuto e il danno effettivamente applicato.</summary>
        public event Action<DamageInfo, float> Damaged;

        /// <summary>Emesso una sola volta.</summary>
        public event Action Died;

        public float Current => _model.Current;
        public float Max => _model.Max;
        public bool IsDead => _model.IsDead;

        private void Awake()
        {
            _model = new HealthModel(_maxHealth);
        }

        public void TakeDamage(in DamageInfo info)
        {
            // Gli eventi si inoltrano da qui e non iscrivendosi a quelli del model:
            // nessun += da bilanciare, e l'ordine (vita, colpo, morte) è deciso in un punto solo.
            bool wasDead = _model.IsDead;
            float applied = _model.ApplyDamage(info.Amount);
            if (applied <= 0f)
            {
                return;
            }

            HealthChanged?.Invoke(_model.Current, _model.Max);
            Damaged?.Invoke(info, applied);

            if (!wasDead && _model.IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}
