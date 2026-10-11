using System;
using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Un proiettile magico (D7 della M9): vola dritto, colpisce il primo bersaglio sulla sua strada
    /// con un raggio a sfera e si ferma sui muri, poi esplode e torna al pool. I muri li dice il
    /// NavMesh e non i collider: dalla M8 i muri alti dalla parte della camera sono spenti, con i loro
    /// collider, mentre il NavMesh è cotto con tutti (trappola 6 della scheda). Niente rigidbody: a
    /// velocità alta una collisione fisica si può perdere tra due frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Il raggio della sfera che cerca i bersagli.")]
        [SerializeField, Min(0.01f)] private float _radius = 0.3f;

        [Tooltip("Le particelle del volo: si spengono all'impatto lasciando morire quelle già nate.")]
        [SerializeField] private ParticleSystem _trail;

        [Tooltip("Il nucleo luminoso, che sparisce all'impatto.")]
        [SerializeField] private ParticleSystem _core;

        [Tooltip("Lo scoppio all'impatto.")]
        [SerializeField] private ParticleSystem _burst;

        [SerializeField] private Light _light;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _impactClip;

        [Tooltip("Quanto dura l'esplosione prima di tornare al pool.")]
        [SerializeField, Min(0.05f)] private float _burstDuration = 0.6f;

        // condiviso: un proiettile alla volta fa la sua ricerca, nel thread principale
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        private Vector3 _direction;
        private float _speed;
        private float _range;
        private float _travelled;
        private float _damage;
        private GameObject _source;
        private int _targetLayers;
        private Action<Projectile> _release;
        private float _lightIntensity;
        private float _burstTimer;
        private float _groundY;

        /// <summary>Esploso, aspetta di tornare al pool.</summary>
        public bool IsExploding { get; private set; }

        /// <summary>Annunciato all'impatto: chi è stato colpito (null su un muro o a fine corsa).</summary>
        public event Action<Projectile, IDamageable> Exploded;

        private void Awake()
        {
            _lightIntensity = _light != null ? _light.intensity : 0f;
        }

        /// <summary>Parte da <paramref name="from"/> verso <paramref name="direction"/>; <paramref name="release"/> lo rimette nel pool.</summary>
        public void Launch(Vector3 from, Vector3 direction, float speed, float range, float damage, GameObject source, int targetLayers, Action<Projectile> release)
        {
            transform.position = from;
            _direction = direction.normalized;
            transform.rotation = Quaternion.LookRotation(_direction);
            _speed = speed;
            _range = range;
            _travelled = 0f;
            _damage = damage;
            _source = source;
            _groundY = source != null ? source.transform.position.y : from.y;
            _targetLayers = targetLayers;
            _release = release;
            IsExploding = false;

            // ripreso dal pool: niente scia né luce del lancio di prima (trappola 1)
            if (_light != null)
            {
                _light.intensity = _lightIntensity;
                _light.enabled = true;
            }

            _trail.Clear();
            _core.Clear();
            _burst.Clear();
            _trail.Play();
            _core.Play();
        }

        private void Update()
        {
            if (IsExploding)
            {
                _burstTimer -= Time.deltaTime;
                if (_light != null)
                {
                    _light.intensity = _lightIntensity * Mathf.Clamp01(_burstTimer / _burstDuration) * 1.5f;
                }

                if (_burstTimer <= 0f)
                {
                    _release?.Invoke(this);
                }

                return;
            }

            float step = Mathf.Min(_speed * Time.deltaTime, _range - _travelled);
            Vector3 from = transform.position;
            if (TryHitTarget(from, step, out IDamageable target, out Vector3 point))
            {
                target.TakeDamage(new DamageInfo(_damage, DamageType.Fire, _source));
                Explode(point, target);
                return;
            }

            Vector3 to = from + _direction * step;
            if (NavMesh.Raycast(Ground(from), Ground(to), out NavMeshHit wall, NavMesh.AllAreas))
            {
                Explode(new Vector3(wall.position.x, from.y, wall.position.z), null);
                return;
            }

            transform.position = to;
            _travelled += step;
            if (_travelled >= _range)
            {
                Explode(to, null);
            }
        }

        // il primo bersaglio vivo sulla strada, senza il lanciatore
        private bool TryHitTarget(Vector3 from, float step, out IDamageable target, out Vector3 point)
        {
            target = null;
            point = from;
            int count = Physics.SphereCastNonAlloc(from, _radius, _direction, Hits, step, _targetLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].collider.GetComponentInParent<Health>();
                if (health == null || health.IsDead || health.gameObject == _source || Hits[i].distance >= best)
                {
                    continue;
                }

                best = Hits[i].distance;
                target = health;
                point = Hits[i].distance > 0f ? Hits[i].point : from;
            }

            return target != null;
        }

        // le posizioni sul pavimento: il proiettile vola all'altezza del petto, il NavMesh sta a terra.
        // L'altezza è quella del lanciatore al lancio, che nel frattempo può essere morto
        private Vector3 Ground(Vector3 position)
        {
            return new Vector3(position.x, _groundY, position.z);
        }

        private void Explode(Vector3 point, IDamageable target)
        {
            IsExploding = true;
            transform.position = point;
            _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _core.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _burst.Play();
            if (_audio != null && _impactClip != null)
            {
                _audio.PlayOneShot(_impactClip);
            }

            _burstTimer = _burstDuration;
            Exploded?.Invoke(this, target);
        }
    }
}
