using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Il pozzo della vita o del mana (D7 della M13): riempie tutta la vita, o tutto il mana, una volta
    /// sola. Usato, il liquido si abbassa fino al fondo del bacino.
    /// </summary>
    [DisallowMultipleComponent]
    public class Well : OneUseFeature
    {
        [Tooltip("Il pozzo del mana; senza, quello della vita.")]
        [SerializeField] private bool _mana;

        [Tooltip("Il pelo del liquido: usato, scende.")]
        [SerializeField] private Transform _liquid;

        [Tooltip("Di quanto scende il liquido, in metri.")]
        [SerializeField, Min(0f)] private float _drop = 0.30f;

        [SerializeField, Min(0.05f)] private float _drainDuration = 1.2f;

        private Vector3 _fullPosition;

        public bool IsMana => _mana;

        /// <summary>Il pelo del liquido, per i test.</summary>
        public Transform Liquid => _liquid;

        protected override string LabelKey => _mana ? "well.mana" : "well.life";

        protected override bool TryApply(GameObject user)
        {
            if (_mana)
            {
                if (!user.TryGetComponent(out PlayerMagic magic))
                {
                    return false;
                }

                magic.Mana.Fill();
                return true;
            }

            if (!user.TryGetComponent(out Health health) || health.IsDead)
            {
                return false;
            }

            health.Heal(health.Max);
            return true;
        }

        protected override void ShowSpent(bool animate)
        {
            base.ShowSpent(animate);
            if (_liquid == null)
            {
                return;
            }

            if (animate)
            {
                StartCoroutine(Drain());
            }
            else
            {
                _liquid.localPosition = _fullPosition + Vector3.down * _drop;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_liquid != null)
            {
                _fullPosition = _liquid.localPosition;
            }
        }

        private IEnumerator Drain()
        {
            Vector3 empty = _fullPosition + Vector3.down * _drop;
            for (float t = 0f; t < _drainDuration; t += Time.deltaTime)
            {
                _liquid.localPosition = Vector3.Lerp(_fullPosition, empty, Mathf.SmoothStep(0f, 1f, t / _drainDuration));
                yield return null;
            }

            _liquid.localPosition = empty;
        }
    }
}
