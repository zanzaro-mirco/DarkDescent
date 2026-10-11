using DarkDescent.Characters;
using DarkDescent.Core;
using DarkDescent.Player;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Quello che gli incantesimi del cavaliere fanno nel mondo (M9). Al lancio il cavaliere si ferma,
    /// si gira verso il punto e fa il gesto del lancio; il proiettile parte quando la mano arriva
    /// avanti. Il danno viene dalla Magia, con un flusso di numeri casuali suo (trappola 3 della scheda).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMagic))]
    public class PlayerSpells : MonoBehaviour
    {
        [Tooltip("I nemici: i soli che i proiettili del cavaliere colpiscono.")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Altezza da terra da cui parte e a cui vola il proiettile: sopra le balaustre basse.")]
        [SerializeField, Min(0f)] private float _launchHeight = 1.1f;

        [Tooltip("Distanza dal centro del cavaliere da cui parte il proiettile, davanti a lui.")]
        [SerializeField, Min(0f)] private float _launchForward = 0.5f;

        [Tooltip("Secondi tra il lancio e la partenza del proiettile: il momento in cui la mano arriva avanti.")]
        [SerializeField, Min(0f)] private float _releaseDelay = 0.25f;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il soffio del lancio.")]
        [SerializeField] private AudioClip _castClip;

        private PlayerMagic _magic;
        private CharacterStats _stats;
        private PlayerMotor _motor;
        private CharacterAnimatorDriver _animator;
        private ProjectilePools _pools;
        private IRandomSource _random;

        // un proiettile che aspetta la mano: uno solo, il lancio dopo fa partire subito quello prima
        private SpellDefinition _pendingSpell;
        private Vector3 _pendingTarget;
        private float _pendingTimer;

        public void Bind(ProjectilePools pools, IRandomSource random)
        {
            _pools = pools;
            _random = random;
        }

        private void Awake()
        {
            _magic = GetComponent<PlayerMagic>();
            _stats = GetComponent<CharacterStats>();
            _motor = GetComponent<PlayerMotor>();
            _animator = GetComponent<CharacterAnimatorDriver>();
        }

        private void OnEnable()
        {
            _magic.Cast += HandleCast;
        }

        private void OnDisable()
        {
            _magic.Cast -= HandleCast;
            _pendingSpell = null;
        }

        private void Update()
        {
            if (_pendingSpell == null)
            {
                return;
            }

            _pendingTimer -= Time.deltaTime;
            if (_pendingTimer <= 0f)
            {
                Release();
            }
        }

        private void HandleCast(SpellDefinition spell, Vector3 target)
        {
            if (_pendingSpell != null)
            {
                Release();
            }

            if (_motor != null)
            {
                _motor.Stop();
            }

            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flat);
            }

            if (_animator != null)
            {
                _animator.PlayCast();
            }

            if (_audio != null && _castClip != null)
            {
                _audio.PlayOneShot(_castClip);
            }

            switch (spell.Kind)
            {
                case SpellKind.Projectile:
                    _pendingSpell = spell;
                    _pendingTarget = target;
                    _pendingTimer = _releaseDelay;
                    break;
            }
        }

        private void Release()
        {
            var spell = _pendingSpell;
            _pendingSpell = null;
            if (_pools == null || spell.Effect == null)
            {
                return;
            }

            Vector3 from = transform.position + Vector3.up * _launchHeight;
            Vector3 aim = _pendingTarget - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
            }

            // in piano, all'altezza del petto: il dardo passa sopra le balaustre e colpisce il busto
            Vector3 direction = aim.normalized;
            from += direction * _launchForward;
            float damage = spell.RollDamage(_stats.Magic, _random);
            _pools.Launch(spell.Effect, from, direction, spell.Speed, spell.Range, damage, gameObject, _targetLayers);
        }
    }
}
