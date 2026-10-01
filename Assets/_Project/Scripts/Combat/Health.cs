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

        public float Current => Model.Current;
        public float Max => Model.Max;
        public bool IsDead => Model.IsDead;

        // Creato al primo accesso, non in Awake: chi si collega in Awake da un altro oggetto
        // (il composition root, la sfera della vita) può arrivare prima, perché l'ordine
        // degli Awake tra oggetti diversi non è garantito.
        private HealthModel Model => _model ??= new HealthModel(_maxHealth);

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
    }
}
