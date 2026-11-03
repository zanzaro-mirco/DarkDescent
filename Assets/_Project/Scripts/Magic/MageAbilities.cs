using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Player;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Quello che le abilità del Mago fanno nel mondo, oltre ai proiettili, alle aree e ai potenziamenti di
    /// <see cref="PlayerSpells"/> (passo 21.4 della M21): il Passo di luce porta subito sul punto indicato,
    /// passando sopra i nemici ma non sui muri; lo Scudo di mana fa pagare il danno al mana; la Meteora cade dopo
    /// un attimo; il Muro di fiamme e la Tempesta di neve restano per un po' e colpiscono a ogni battito chi ci
    /// sta dentro. I numeri vengono dall'asset con i nodi dell'albero (<see cref="PlayerMagic.AbilityValue"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMagic), typeof(PlayerSpells))]
    public class MageAbilities : MonoBehaviour
    {
        [Tooltip("I nemici: i soli che le abilità colpiscono.")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Altezza da terra a cui si cercano i nemici attorno, come per l'Anello di fuoco.")]
        [SerializeField, Min(0f)] private float _searchHeight = 1.1f;

        [Tooltip("Metri dal bordo di un muro a cui il Passo di luce si ferma.")]
        [SerializeField, Min(0f)] private float _wallMargin = 0.4f;

        [Tooltip("Metri dalla riga di fiamme entro cui un nemico brucia.")]
        [SerializeField, Min(0f)] private float _wallHalfWidth = 0.8f;

        [Tooltip("Metri da cui parte a cadere la Meteora.")]
        [SerializeField, Min(1f)] private float _meteorHeight = 12f;

        [Header("Effetti e suoni")]
        [SerializeField] private AudioSource _audio;

        [Tooltip("Il lampo del Passo di luce, alla partenza e all'arrivo.")]
        [SerializeField] private AreaEffect _blinkEffect;

        [Tooltip("L'alone dello Scudo di mana, sul Mago finché dura.")]
        [SerializeField] private GameObject _shieldEffect;

        [Tooltip("La meteora che cade: un proiettile di fuoco che scende dall'alto.")]
        [SerializeField] private GameObject _meteorEffect;

        [Tooltip("Lo scoppio della Meteora.")]
        [SerializeField] private AreaEffect _meteorImpact;

        [Tooltip("Le fiamme del Muro, accese lungo la riga a ogni battito.")]
        [SerializeField] private AreaEffect _wallEffect;

        [Tooltip("Il turbine di neve, acceso nel cerchio a ogni battito.")]
        [SerializeField] private AreaEffect _snowEffect;

        [SerializeField] private AudioClip _blinkClip;

        [SerializeField] private AudioClip _shieldClip;

        [SerializeField] private AudioClip _meteorClip;

        // condiviso: una ricerca alla volta, nel thread principale
        private static readonly Collider[] AreaHits = new Collider[32];

        private readonly HashSet<Health> _targets = new HashSet<Health>();
        private readonly List<Meteor> _meteors = new List<Meteor>();
        private readonly List<Zone> _zones = new List<Zone>();
        private readonly Dictionary<AreaEffect, List<AreaEffect>> _effects = new Dictionary<AreaEffect, List<AreaEffect>>();

        private PlayerMagic _magic;
        private PlayerSpells _spells;
        private Health _health;
        private CharacterStats _stats;
        private NavMeshAgent _agent;
        private PlayerMotor _motor;
        private MeleeAttack _attack;
        private CharacterAnimatorDriver _animator;
        private ProjectilePools _pools;

        // lo Scudo di mana in corso
        private SpellDefinition _shield;
        private float _shieldRemaining;
        private AuraEffect _shieldView;

        /// <summary>I secondi che restano allo Scudo di mana; 0 se non c'è.</summary>
        public float ShieldRemaining => _shield != null ? _shieldRemaining : 0f;

        /// <summary>Quante meteore sono in aria e quante aree restano accese: per i test.</summary>
        public int PendingMeteors => _meteors.Count;

        public int ActiveZones => _zones.Count;

        public void Bind(ProjectilePools pools)
        {
            _pools = pools;
        }

        private void Awake()
        {
            _magic = GetComponent<PlayerMagic>();
            _spells = GetComponent<PlayerSpells>();
            _health = GetComponent<Health>();
            _stats = GetComponent<CharacterStats>();
            _agent = GetComponent<NavMeshAgent>();
            _motor = GetComponent<PlayerMotor>();
            _attack = GetComponent<MeleeAttack>();
            _animator = GetComponent<CharacterAnimatorDriver>();
        }

        private void OnEnable()
        {
            _magic.Cast += HandleCast;
            _health.Died += EndAll;
        }

        private void OnDisable()
        {
            _magic.Cast -= HandleCast;
            _health.Died -= EndAll;
            EndAll();
        }

        private void HandleCast(SpellDefinition spell, Vector3 point)
        {
            if (!spell.IsMageAbility || spell.Kind == SpellKind.ArcaneBolt)
            {
                return;
            }

            // la Meteora e le aree cadono entro la gittata: il cursore più in là le porta al limite
            if ((spell.Kind == SpellKind.Meteor || spell.Kind == SpellKind.Zone) && spell.Range > 0f)
            {
                Vector3 away = point - transform.position;
                away.y = 0f;
                if (away.magnitude > spell.Range)
                {
                    point = transform.position + away.normalized * spell.Range;
                }
            }

            Cast(spell, point);
            switch (spell.Kind)
            {
                case SpellKind.Teleport:
                    Blink(spell, point);
                    break;
                case SpellKind.ManaShield:
                    RaiseShield(spell);
                    break;
                case SpellKind.Meteor:
                    CallMeteor(spell, point);
                    break;
                case SpellKind.Zone:
                    OpenZone(spell, point);
                    break;
            }
        }

        // il gesto: ci si ferma e ci si gira dove si lancia; lo Scudo di mana non ha una direzione
        private void Cast(SpellDefinition spell, Vector3 point)
        {
            if (_motor != null)
            {
                _motor.Stop();
            }

            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (spell.NeedsPoint && flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flat);
            }

            if (_animator != null)
            {
                _animator.PlayCast();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_shield != null)
            {
                _shieldRemaining -= deltaTime;
                if (_shieldRemaining <= 0f)
                {
                    EndShield();
                }
            }

            for (int i = _meteors.Count - 1; i >= 0; i--)
            {
                var meteor = _meteors[i];
                meteor.Timer -= deltaTime;
                if (!meteor.Fell && meteor.Timer <= meteor.Spell.Delay)
                {
                    Drop(meteor);
                }

                if (meteor.Timer <= 0f)
                {
                    _meteors.RemoveAt(i);
                    Impact(meteor);
                }
            }

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                var zone = _zones[i];
                zone.Remaining -= deltaTime;
                zone.Tick -= deltaTime;
                if (zone.Tick <= 0f && zone.Remaining > -zone.Interval)
                {
                    zone.Tick += zone.Interval;
                    Beat(zone);
                }

                if (zone.Remaining <= 0f)
                {
                    _zones.RemoveAt(i);
                }
            }
        }

        // ---- Passo di luce ------------------------------------------------------------------------------------------

        private void Blink(SpellDefinition spell, Vector3 point)
        {
            Vector3 from = transform.position;
            Vector3 toward = point - from;
            toward.y = 0f;
            float reach = Mathf.Min(toward.magnitude, spell.Range);
            Vector3 destination = reach > 0.01f ? from + toward.normalized * reach : from;
            destination = Walkable(from, destination);

            if (_attack != null)
            {
                // il colpo in corso non arriva più: ci si è spostati
                _attack.ClearTarget();
                _attack.Interrupt(0.05f);
            }

            PlayEffect(_blinkEffect, from, 1.2f);
            if (_agent != null && _agent.enabled)
            {
                _agent.Warp(destination);
            }
            else
            {
                transform.position = destination;
            }

            PlayEffect(_blinkEffect, destination, 1.2f);
            Play(_blinkClip);
        }

        // il punto più lontano raggiungibile sulla strada: i nemici si passano sopra, i muri no
        private Vector3 Walkable(Vector3 from, Vector3 wanted)
        {
            if (NavMesh.Raycast(from, wanted, out NavMeshHit wall, NavMesh.AllAreas))
            {
                Vector3 back = (from - wanted).normalized * _wallMargin;
                wanted = wall.position + new Vector3(back.x, 0f, back.z);
            }

            return NavMesh.SamplePosition(wanted, out NavMeshHit ground, 2f, NavMesh.AllAreas) ? ground.position : from;
        }

        // ---- Scudo di mana ------------------------------------------------------------------------------------------

        private void RaiseShield(SpellDefinition spell)
        {
            EndShield();
            _shield = spell;
            _shieldRemaining = _magic.AbilityValue(spell, AbilityParam.Duration);
            _health.Absorber = Absorb;
            Play(_shieldClip);
            if (_shieldEffect != null)
            {
                if (_shieldView == null)
                {
                    // figlio del Mago: l'alone lo segue
                    _shieldView = Instantiate(_shieldEffect, transform).GetComponent<AuraEffect>();
                    _shieldView.transform.localPosition = Vector3.zero;
                }

                _shieldView.Play();
            }
        }

        // il danno che arriva scala il mana, a tanti punti per ogni punto di danno, finché il mana basta; il resto passa alla vita
        private float Absorb(float amount)
        {
            if (_shield == null || _shield.ManaPerDamage <= 0f)
            {
                return amount;
            }

            float ratio = _shield.ManaPerDamage;
            float mana = _magic.Mana.Current;
            float cost = amount * ratio;
            if (mana >= cost)
            {
                _magic.Mana.TrySpend(cost);
                return 0f;
            }

            // il mana che resta copre solo una parte del colpo
            _magic.Mana.TrySpend(mana);
            return amount - mana / ratio;
        }

        private void EndShield()
        {
            if (_shield == null)
            {
                return;
            }

            _shield = null;
            _shieldRemaining = 0f;
            _health.Absorber = null;
            if (_shieldView != null)
            {
                _shieldView.Stop();
            }
        }

        // ---- Meteora ------------------------------------------------------------------------------------------------

        private void CallMeteor(SpellDefinition spell, Vector3 point)
        {
            point = Ground(point);
            var first = new Meteor { Spell = spell, Point = point, Timer = spell.Delay };
            _meteors.Add(first);
            if (spell.Delay > 0f)
            {
                Drop(first);
            }

            // la Meteora doppia: le altre cadono sullo stesso punto, a un secondo l'una dall'altra
            int extra = Mathf.RoundToInt(_magic.AbilityValue(spell, AbilityParam.Echo));
            for (int i = 1; i <= extra; i++)
            {
                _meteors.Add(new Meteor { Spell = spell, Point = point, Timer = spell.Delay + PlayerSpells.EchoDelay * i });
            }
        }

        // la meteora scende dritta sul punto: senza spostamento di lato, il NavMesh non la ferma prima
        private void Drop(Meteor meteor)
        {
            meteor.Fell = true;
            if (_meteorEffect != null && _pools != null && meteor.Spell.Delay > 0f)
            {
                Vector3 from = meteor.Point + Vector3.up * _meteorHeight;
                _pools.Launch(_meteorEffect, from, Vector3.down, _meteorHeight / meteor.Spell.Delay, _meteorHeight + 1f, 0f, gameObject, 0);
            }
        }

        private void Impact(Meteor meteor)
        {
            float radius = _magic.AbilityValue(meteor.Spell, AbilityParam.Radius);
            PlayEffect(_meteorImpact, meteor.Point, radius);
            Play(_meteorClip);
            int count = Physics.OverlapSphereNonAlloc(meteor.Point + Vector3.up * _searchHeight, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
            Hit(meteor.Spell, count);
        }

        // ---- Aree che restano ---------------------------------------------------------------------------------------

        private void OpenZone(SpellDefinition spell, Vector3 point)
        {
            point = Ground(point);
            Vector3 along = Vector3.right;
            if (spell.Length > 0f)
            {
                // il muro attraversa la direzione del lancio: una riga ai due lati del punto
                Vector3 facing = point - transform.position;
                facing.y = 0f;
                along = facing.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, facing.normalized) : Vector3.right;
            }

            float interval = Mathf.Max(0.1f, spell.Tick);
            _zones.Add(new Zone
            {
                Spell = spell,
                Center = point,
                Along = along,
                Remaining = _magic.AbilityValue(spell, AbilityParam.Duration),
                Interval = interval,
                Tick = 0f,
            });
        }

        /// <summary>
        /// Le fiamme che la Palla di fuoco lascia a terra con la Marea di fuoco (M21): un cerchio che brucia per tanti secondi,
        /// a ogni battito con una parte del danno della Palla.
        /// </summary>
        public void OpenGround(SpellDefinition spell, Vector3 point, float radius, float seconds)
        {
            _zones.Add(new Zone
            {
                Spell = spell,
                Center = Ground(point),
                Along = Vector3.right,
                Remaining = seconds,
                Interval = GroundInterval,
                Tick = GroundInterval,
                Radius = radius,
                Factor = GroundFactor,
            });
        }

        // a ogni battito di 0,5 s le fiamme a terra fanno il 10% del danno della Palla: 4 s sono un 80% in più su chi resta
        private const float GroundInterval = 0.5f;
        private const float GroundFactor = 0.1f;

        // un battito: chi sta dentro prende il danno e il rallentamento, e le fiamme o la neve si accendono
        private void Beat(Zone zone)
        {
            var spell = zone.Spell;
            int count;
            if (zone.Radius > 0f)
            {
                count = Physics.OverlapSphereNonAlloc(zone.Center + Vector3.up * _searchHeight, zone.Radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 inside = Random.insideUnitCircle * zone.Radius * 0.7f;
                    PlayEffect(_wallEffect, zone.Center + new Vector3(inside.x, 0f, inside.y), 0.9f);
                }
            }
            else if (spell.Length > 0f)
            {
                Vector3 half = zone.Along * (spell.Length * 0.5f);
                Vector3 a = zone.Center - half + Vector3.up * _searchHeight;
                Vector3 b = zone.Center + half + Vector3.up * _searchHeight;
                count = Physics.OverlapCapsuleNonAlloc(a, b, _wallHalfWidth, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
                int flames = Mathf.Max(2, Mathf.CeilToInt(spell.Length / 1.7f));
                for (int i = 0; i < flames; i++)
                {
                    float t = flames == 1 ? 0.5f : (float)i / (flames - 1);
                    PlayEffect(_wallEffect, zone.Center - half + zone.Along * (spell.Length * t), 0.9f);
                }
            }
            else
            {
                float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
                count = Physics.OverlapSphereNonAlloc(zone.Center + Vector3.up * _searchHeight, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 inside = Random.insideUnitCircle * radius * 0.7f;
                    PlayEffect(_snowEffect, zone.Center + new Vector3(inside.x, 0f, inside.y), 1.6f);
                }
            }

            Hit(spell, count, zone.Factor);
        }

        // tutti i nemici vivi trovati, una volta sola anche con più collider, ognuno con il suo tiro
        private void Hit(SpellDefinition spell, int count, float factor = 1f)
        {
            _targets.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = AreaHits[i].GetComponentInParent<Health>();
                if (target == null || target.IsDead || target.gameObject == gameObject || !_targets.Add(target))
                {
                    continue;
                }

                if (spell.MaxDamage > 0)
                {
                    target.TakeDamage(new DamageInfo(_spells.RollDamage(spell) * factor, spell.DamageType, gameObject));
                }

                _spells.ApplyFrost(spell, target);
                _spells.ReportStruck(spell, target);
            }

            _targets.Clear();
        }

        // ---- Aiuti --------------------------------------------------------------------------------------------------

        // il punto sul pavimento del NavMesh sotto quello del cursore
        private static Vector3 Ground(Vector3 point)
        {
            return NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : point;
        }

        // più copie dello stesso effetto: una accesa non si ferma perché ne serve un'altra
        private void PlayEffect(AreaEffect prefab, Vector3 center, float radius)
        {
            if (prefab == null)
            {
                return;
            }

            if (!_effects.TryGetValue(prefab, out var copies))
            {
                copies = new List<AreaEffect>();
                _effects[prefab] = copies;
            }

            AreaEffect free = null;
            foreach (var copy in copies)
            {
                if (copy != null && !copy.IsPlaying)
                {
                    free = copy;
                    break;
                }
            }

            if (free == null)
            {
                // fuori dal Mago: l'effetto resta dove è nato anche se lui si muove
                free = Instantiate(prefab);
                copies.Add(free);
            }

            free.Play(center, radius);
        }

        private void Play(AudioClip clip)
        {
            if (_audio != null && clip != null)
            {
                _audio.PlayOneShot(clip);
            }
        }

        private void EndAll()
        {
            EndShield();
            _meteors.Clear();
            _zones.Clear();
        }

        private void OnDestroy()
        {
            foreach (var copies in _effects.Values)
            {
                foreach (var copy in copies)
                {
                    if (copy != null)
                    {
                        Destroy(copy.gameObject);
                    }
                }
            }
        }

        private sealed class Meteor
        {
            public SpellDefinition Spell;
            public Vector3 Point;
            public float Timer;
            public bool Fell;
        }

        private sealed class Zone
        {
            public SpellDefinition Spell;
            public Vector3 Center;
            public Vector3 Along;
            public float Remaining;
            public float Interval;
            public float Tick;

            // le fiamme a terra della Marea di fuoco: un cerchio di questo raggio, a questa parte del danno dell'abilità
            public float Radius;
            public float Factor = 1f;
        }
    }
}
