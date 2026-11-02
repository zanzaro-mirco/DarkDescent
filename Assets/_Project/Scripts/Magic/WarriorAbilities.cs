using System;
using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Player;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Quello che le abilità del Guerriero fanno nel mondo (passo 20.4 della M20). I colpi (Colpo
    /// possente, Esecuzione, Colpo di scudo) vanno al nemico sotto il cursore, o al più vicino a
    /// portata, dopo il gesto; la Carica corre sul nemico tra 3 e 10 m; il Salto vola sul punto
    /// indicato; Turbine, Grido, Muro di scudi, Ultimo baluardo e Tempesta d'acciaio partono dal
    /// cavaliere. Il danno è quello di un colpo dell'arma in percentuale, con il critico e il rubavita,
    /// tirato da un flusso di numeri suo (trappola 3 della M9). I numeri vengono dall'asset con i nodi
    /// dell'albero (<see cref="PlayerMagic.AbilityValue"/>). Prima che il mana si spenda controlla che
    /// ci sia qualcosa da colpire.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMagic), typeof(MeleeAttack))]
    public class WarriorAbilities : MonoBehaviour, IAbilityTargeting
    {
        // gli stati del controller con i gesti delle abilità (TalentTools.InstallAbilityStates)
        public const string StrikeState = "Strike";
        public const string SpinState = "Spin";
        public const string BashState = "ShieldBash";
        public const string WallState = "ShieldWall";
        public const string ShoutState = "Shout";
        public const string LeapState = "Leap";

        [Tooltip("I nemici: i soli che le abilità colpiscono.")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Metri dal punto sotto il cursore entro cui si cerca il nemico, se il cursore non ne ha uno sopra.")]
        [SerializeField, Min(0f)] private float _pickRadius = 1.5f;

        [Tooltip("Metri oltre la portata dell'arma per un colpo d'abilità: il passo avanti del gesto.")]
        [SerializeField, Min(0f)] private float _reachBonus = 0.8f;

        [Tooltip("Secondi tra il gesto e il colpo: la lama che arriva.")]
        [SerializeField, Min(0f)] private float _strikeDelay = 0.3f;

        [Tooltip("La Carica corre a tante volte la velocità normale.")]
        [SerializeField, Range(1f, 4f)] private float _chargeSpeed = 3f;

        [Tooltip("Secondi dopo cui la Carica si ferma anche senza essere arrivata.")]
        [SerializeField, Min(0.1f)] private float _chargeTimeout = 1.5f;

        [Tooltip("Secondi in aria del Salto.")]
        [SerializeField, Min(0.1f)] private float _leapTime = 0.6f;

        [Tooltip("Metri d'altezza del Salto, a metà strada.")]
        [SerializeField, Min(0f)] private float _leapHeight = 1.5f;

        [Tooltip("Altezza da terra a cui si cercano i nemici attorno, come per l'Anello di fuoco.")]
        [SerializeField, Min(0f)] private float _searchHeight = 1.1f;

        [Header("Effetti e suoni")]
        [SerializeField] private AudioSource _audio;

        [Tooltip("Il fendente dei colpi e della Carica che arriva.")]
        [SerializeField] private AudioClip _swingClip;

        [Tooltip("La lama che fischia in cerchio: Turbine e Tempesta d'acciaio.")]
        [SerializeField] private AudioClip _whirlClip;

        [Tooltip("I tintinnii di un nemico stordito.")]
        [SerializeField] private AudioClip _stunClip;

        [SerializeField] private AudioClip _shoutClip;

        [Tooltip("Lo scudo che sbatte: Colpo di scudo e Muro di scudi.")]
        [SerializeField] private AudioClip _shieldClip;

        [Tooltip("L'atterraggio del Salto.")]
        [SerializeField] private AudioClip _landClip;

        [Tooltip("Il cerchio che si allarga dal cavaliere: Turbine, Tempesta d'acciaio, atterraggio del Salto.")]
        [SerializeField] private AreaEffect _spinEffect;

        [Tooltip("L'onda del Grido di battaglia e dell'Ultimo baluardo.")]
        [SerializeField] private AreaEffect _shoutEffect;

        [Tooltip("Le stelle sopra un nemico stordito: si accendono per la durata.")]
        [SerializeField] private GameObject _stunEffect;

        [Tooltip("Altezza delle stelle sopra i piedi del nemico.")]
        [SerializeField, Min(0f)] private float _stunEffectHeight = 2.1f;

        // condiviso: una ricerca alla volta, nel thread principale
        private static readonly Collider[] AreaHits = new Collider[32];

        private readonly HashSet<Health> _areaTargets = new HashSet<Health>();
        private readonly List<Health> _struck = new List<Health>();
        private readonly Dictionary<object, float> _timed = new Dictionary<object, float>();
        private readonly List<object> _ended = new List<object>();
        private readonly Dictionary<AreaEffect, AreaEffect> _effects = new Dictionary<AreaEffect, AreaEffect>();

        // le sorgenti dei modificatori a tempo sulla scheda: si tolgono con la loro durata
        private readonly object _shout = new object();
        private readonly object _lastStand = new object();
        private readonly object _wall = new object();
        private readonly object _bladestorm = new object();

        private PlayerMagic _magic;
        private MeleeAttack _attack;
        private Health _health;
        private CharacterStats _stats;
        private NavMeshAgent _agent;
        private PlayerMotor _motor;
        private PlayerController _controller;
        private CharacterAnimatorDriver _animator;
        private ShieldBlock _block;
        private MovementSlow _speed;
        private IRandomSource _random;

        // il bersaglio scelto da CanUse, per il lancio che segue nello stesso frame
        private Health _chosen;
        private Vector3 _chosenPoint;

        // un colpo che aspetta la lama
        private SpellDefinition _pending;
        private Health _pendingTarget;
        private float _pendingTimer;

        // la Carica in corsa
        private SpellDefinition _charge;
        private Health _chargeTarget;
        private float _chargeTimer;

        // il Salto in volo
        private SpellDefinition _leap;
        private Vector3 _leapFrom;
        private Vector3 _leapTo;
        private float _leapTimer;

        // la Tempesta d'acciaio: il colpo che viene
        private SpellDefinition _storm;
        private float _stormTick;

        // la velocità del Muro di scudi, finché dura
        private float _wallFactor = 1f;

        /// <summary>Una carica, un salto o un colpo in corso: il cavaliere è impegnato.</summary>
        public bool IsBusy => _pending != null || _charge != null || _leap != null;

        /// <summary>La Tempesta d'acciaio gira.</summary>
        public bool IsStorming => _storm != null;

        /// <summary>Un nemico è stato stordito da un'abilità, con i secondi che prende.</summary>
        public event Action<EnemyAI, float> Stunned;

        public void Bind(IRandomSource random)
        {
            _random = random;
        }

        private void Awake()
        {
            _magic = GetComponent<PlayerMagic>();
            _attack = GetComponent<MeleeAttack>();
            _health = GetComponent<Health>();
            _stats = GetComponent<CharacterStats>();
            _agent = GetComponent<NavMeshAgent>();
            _motor = GetComponent<PlayerMotor>();
            _controller = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimatorDriver>();
            _block = GetComponent<ShieldBlock>();
            _speed = GetComponent<MovementSlow>();
        }

        private void OnEnable()
        {
            _magic.SetTargeting(this);
            _magic.Cast += HandleCast;
            _health.Died += EndAll;
        }

        private void OnDisable()
        {
            _magic.Cast -= HandleCast;
            _health.Died -= EndAll;
            _magic.SetTargeting(null);
            EndAll();
        }

        /// <summary>Che cosa fa il lancio, prima del mana: un nemico a portata, lo scudo in mano.</summary>
        public CastResult CanUse(SpellDefinition ability, Vector3 point)
        {
            if (IsBusy)
            {
                return CastResult.Cooldown;
            }

            _chosen = null;
            _chosenPoint = point;
            switch (ability.Kind)
            {
                case SpellKind.Strike:
                case SpellKind.Execute:
                    _chosen = PickInReach(point);
                    return _chosen != null ? CastResult.Ready : CastResult.NoTarget;
                case SpellKind.ShieldBash:
                    if (!HasShield)
                    {
                        return CastResult.NoShield;
                    }

                    _chosen = PickInReach(point);
                    return _chosen != null ? CastResult.Ready : CastResult.NoTarget;
                case SpellKind.ShieldWall:
                    return HasShield ? CastResult.Ready : CastResult.NoShield;
                case SpellKind.Charge:
                    _chosen = PickForCharge(ability, point);
                    return _chosen != null ? CastResult.Ready : CastResult.NoTarget;
                case SpellKind.Leap:
                    _chosenPoint = LeapLanding(ability, point);
                    return CastResult.Ready;
                default:
                    return CastResult.Ready;
            }
        }

        private bool HasShield => _block != null && _block.HasShield;

        private void HandleCast(SpellDefinition spell, Vector3 point)
        {
            if (!spell.IsWarriorAbility)
            {
                return;
            }

            // senza CanUse (un test che lancia a mano) si sceglie qui
            if (_chosen == null && NeedsTarget(spell.Kind))
            {
                CanUse(spell, point);
            }

            switch (spell.Kind)
            {
                case SpellKind.Strike:
                case SpellKind.Execute:
                    BeginStrike(spell, StrikeState);
                    break;
                case SpellKind.ShieldBash:
                    BeginStrike(spell, BashState);
                    Play(_shieldClip);
                    break;
                case SpellKind.Charge:
                    BeginCharge(spell);
                    break;
                case SpellKind.Leap:
                    BeginLeap(spell);
                    break;
                case SpellKind.Whirlwind:
                    Whirl(spell);
                    break;
                case SpellKind.Bladestorm:
                    BeginStorm(spell);
                    break;
                case SpellKind.Shout:
                    Shout(spell);
                    break;
                case SpellKind.ShieldWall:
                    RaiseWall(spell);
                    break;
                case SpellKind.LastStand:
                    StandFirm(spell);
                    break;
            }

            _chosen = null;
        }

        private static bool NeedsTarget(SpellKind kind) => kind == SpellKind.Strike || kind == SpellKind.Execute || kind == SpellKind.ShieldBash || kind == SpellKind.Charge;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateTimed(deltaTime);

            if (_pending != null)
            {
                _pendingTimer -= deltaTime;
                if (_pendingTarget != null && !_pendingTarget.IsDead)
                {
                    Face(_pendingTarget.transform.position);
                }

                if (_pendingTimer <= 0f)
                {
                    ResolveStrike();
                }
            }

            if (_charge != null)
            {
                UpdateCharge(deltaTime);
            }

            if (_leap != null)
            {
                UpdateLeap(deltaTime);
            }

            if (_storm != null)
            {
                _stormTick -= deltaTime;
                if (_stormTick <= 0f)
                {
                    StormHit();
                }
            }
        }

        // i colpi: il gesto, poi la lama, se il nemico è ancora lì
        private void BeginStrike(SpellDefinition spell, string state)
        {
            _pending = spell;
            _pendingTarget = _chosen;
            _pendingTimer = _strikeDelay;
            StopAndCommit(_strikeDelay);
            if (_pendingTarget != null)
            {
                Face(_pendingTarget.transform.position);
            }

            PlayState(state);
            Play(_swingClip);
        }

        private void ResolveStrike()
        {
            var spell = _pending;
            var target = _pendingTarget;
            _pending = null;
            _pendingTarget = null;
            if (target == null || target.IsDead || EdgeDistance(target) > Reach + 0.5f)
            {
                return;
            }

            float percent = _magic.AbilityValue(spell, AbilityParam.Damage);
            if (spell.Kind == SpellKind.Execute && target.Current < target.Max * _magic.AbilityValue(spell, AbilityParam.Threshold) / 100f)
            {
                // sotto la soglia il colpo forte, con i bonus del danno dell'Esecuzione
                percent = spell.BonusPercent + _magic.AbilityValue(spell, AbilityParam.Damage) - spell.WeaponPercent;
            }

            float stun = _magic.AbilityValue(spell, AbilityParam.Stun);
            HitOne(target, percent, stun);

            // lo Scudo tonante: il colpo di scudo prende anche chi sta attorno al bersaglio
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            if (radius > 0f)
            {
                HitAround(target.transform.position, radius, percent, stun, target);
            }
        }

        // la Carica: di corsa sul bersaglio, poi il colpo e lo stordimento
        private void BeginCharge(SpellDefinition spell)
        {
            if (_chosen == null)
            {
                return;
            }

            _charge = spell;
            _chargeTarget = _chosen;
            _chargeTimer = _chargeTimeout;
            _attack.Interrupt(0.05f);
            if (_speed != null)
            {
                _speed.SetAbilityFactor(_chargeSpeed);
            }

            ChaseCharge();
        }

        private void UpdateCharge(float deltaTime)
        {
            _chargeTimer -= deltaTime;
            if (_chargeTarget == null || _chargeTarget.IsDead || _chargeTimer <= 0f)
            {
                EndCharge();
                return;
            }

            if (EdgeDistance(_chargeTarget) > _attack.Weapon.Range)
            {
                ChaseCharge();
                return;
            }

            var spell = _charge;
            var target = _chargeTarget;
            EndCharge();
            Face(target.transform.position);
            PlayState(StrikeState);
            Play(_swingClip);
            float percent = _magic.AbilityValue(spell, AbilityParam.Damage);
            float stun = _magic.AbilityValue(spell, AbilityParam.Stun);
            HitOne(target, percent, stun);

            // la Carica furiosa: tutti quelli attorno all'arrivo
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            if (radius > 0f)
            {
                HitAround(transform.position, radius, percent, stun, target);
                ShowArea(_spinEffect, transform.position, radius);
            }
        }

        private void ChaseCharge()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(_chargeTarget.transform.position);
            }
        }

        private void EndCharge()
        {
            _charge = null;
            _chargeTarget = null;
            ApplyFactor();

            if (_motor != null)
            {
                _motor.Stop();
            }
        }

        // il Salto: in aria fino al punto, poi il colpo a tutti attorno
        private void BeginLeap(SpellDefinition spell)
        {
            StopAndCommit(_leapTime);
            _leap = spell;
            _leapFrom = transform.position;
            _leapTo = _chosenPoint;
            _leapTimer = 0f;
            Face(_leapTo);
            PlayState(LeapState);

            // in volo l'agent non tiene il cavaliere a terra: lo si riaccende all'atterraggio
            _agent.enabled = false;
        }

        private void UpdateLeap(float deltaTime)
        {
            _leapTimer += deltaTime;
            float t = Mathf.Clamp01(_leapTimer / _leapTime);
            Vector3 position = Vector3.Lerp(_leapFrom, _leapTo, t);
            position.y += _leapHeight * 4f * t * (1f - t);
            transform.position = position;
            if (t < 1f)
            {
                return;
            }

            var spell = _leap;
            Land();
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            HitAround(transform.position, radius, _magic.AbilityValue(spell, AbilityParam.Damage), _magic.AbilityValue(spell, AbilityParam.Stun), null);
            ShowArea(_spinEffect, transform.position, radius);
            Play(_landClip);
        }

        private void Land()
        {
            _leap = null;
            transform.position = _leapTo;
            _agent.enabled = true;
            if (_agent.isOnNavMesh)
            {
                _agent.Warp(_leapTo);
            }
        }

        private void Whirl(SpellDefinition spell)
        {
            StopAndCommit(_strikeDelay);
            PlayState(SpinState);
            Play(_whirlClip);
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            HitAround(transform.position, radius, _magic.AbilityValue(spell, AbilityParam.Damage), 0f, null);
            ShowArea(_spinEffect, transform.position, radius);
        }

        // la Tempesta d'acciaio: un Turbine a ogni colpo, camminando piano
        private void BeginStorm(SpellDefinition spell)
        {
            float duration = _magic.AbilityValue(spell, AbilityParam.Duration);
            _storm = spell;
            _stormTick = 0f;
            _attack.Interrupt(duration);
            StartTimed(_bladestorm, duration);
            ApplyFactor();
        }

        private void StormHit()
        {
            _stormTick = Mathf.Max(0.1f, _storm.Tick);
            PlayState(SpinState);
            Play(_whirlClip);
            float radius = _magic.AbilityValue(_storm, AbilityParam.Radius);
            HitAround(transform.position, radius, _magic.AbilityValue(_storm, AbilityParam.Damage), 0f, null);
            ShowArea(_spinEffect, transform.position, radius);
        }

        private void Shout(SpellDefinition spell)
        {
            PlayState(ShoutState);
            Play(_shoutClip);
            float duration = _magic.AbilityValue(spell, AbilityParam.Duration);
            _stats.Sheet.RemoveModifiersFrom(_shout);
            _stats.Sheet.AddModifier(new StatModifier(StatType.DamageBonus, ModifierKind.Flat, spell.BonusPercent, _shout));
            StartTimed(_shout, duration);

            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            ShowArea(_shoutEffect, transform.position, radius);

            // il Grido che scuote stordisce chi è attorno; il Grido dei padri cura
            float stun = _magic.AbilityValue(spell, AbilityParam.Stun);
            if (stun > 0f)
            {
                StunAround(transform.position, radius, stun);
            }

            float heal = _magic.AbilityValue(spell, AbilityParam.Heal);
            if (heal > 0f)
            {
                _health.Heal(_health.Max * heal / 100f);
            }
        }

        private void RaiseWall(SpellDefinition spell)
        {
            PlayState(WallState);
            Play(_shieldClip);
            _block.BlocksAll = true;
            StartTimed(_wall, _magic.AbilityValue(spell, AbilityParam.Duration));
            _wallFactor = spell.MoveFactor;
            ApplyFactor();
        }

        // l'Ultimo baluardo: la vita in più. La vita attuale sale con il massimo, quindi è già curata
        // quanto ne aggiunge, e alla fine scende di altrettanto, mai sotto 1
        private void StandFirm(SpellDefinition spell)
        {
            PlayState(ShoutState);
            Play(_shoutClip);
            ShowArea(_shoutEffect, transform.position, 2f);
            _stats.Sheet.RemoveModifiersFrom(_lastStand);
            _stats.Sheet.AddModifier(new StatModifier(StatType.MaxLifePercent, ModifierKind.Flat, spell.BonusPercent, _lastStand));
            StartTimed(_lastStand, _magic.AbilityValue(spell, AbilityParam.Duration));
        }

        // la velocità che le abilità in corso tengono: la più lenta, e la Carica sopra tutte
        private void ApplyFactor()
        {
            if (_speed == null || _charge != null)
            {
                return;
            }

            float factor = _timed.ContainsKey(_wall) ? _wallFactor : 1f;
            if (_storm != null)
            {
                factor = Mathf.Min(factor, _storm.MoveFactor);
            }

            _speed.SetAbilityFactor(factor);
        }

        private void StartTimed(object source, float duration)
        {
            _timed[source] = Mathf.Max(0.01f, duration);
        }

        private void UpdateTimed(float deltaTime)
        {
            if (_timed.Count == 0)
            {
                return;
            }

            _ended.Clear();
            foreach (var source in new List<object>(_timed.Keys))
            {
                float left = _timed[source] - deltaTime;
                _timed[source] = left;
                if (left <= 0f)
                {
                    _ended.Add(source);
                }
            }

            foreach (var source in _ended)
            {
                EndTimed(source);
            }
        }

        private void EndTimed(object source)
        {
            if (!_timed.Remove(source))
            {
                return;
            }

            if (source == _shout || source == _lastStand)
            {
                _stats.Sheet.RemoveModifiersFrom(source);
            }
            else if (source == _wall)
            {
                _block.BlocksAll = false;
                _wallFactor = 1f;
            }
            else if (source == _bladestorm)
            {
                _storm = null;
            }

            ApplyFactor();
        }

        /// <summary>I secondi che restano a un effetto a tempo dell'abilità; 0 se non c'è. Per i test e per l'HUD.</summary>
        public float Remaining(SpellKind kind)
        {
            object source = kind == SpellKind.Shout ? _shout : kind == SpellKind.LastStand ? _lastStand : kind == SpellKind.ShieldWall ? _wall : kind == SpellKind.Bladestorm ? _bladestorm : null;
            return source != null && _timed.TryGetValue(source, out float left) ? left : 0f;
        }

        // alla morte o spegnendo il cavaliere: niente resta acceso, e se era in aria torna a terra
        private void EndAll()
        {
            if (_leap != null)
            {
                Land();
            }

            _pending = null;
            _pendingTarget = null;
            if (_charge != null)
            {
                EndCharge();
            }

            foreach (var source in new List<object>(_timed.Keys))
            {
                EndTimed(source);
            }

            _storm = null;
        }

        private void HitOne(Health target, float percent, float stun)
        {
            if (_random == null)
            {
                throw new InvalidOperationException($"{name}: WarriorAbilities senza IRandomSource, lo collega il CompositionRoot");
            }

            _attack.Strike(target, percent, _random);
            if (stun > 0f && !target.IsDead && target.TryGetComponent(out EnemyAI enemy))
            {
                TryStun(enemy, stun);
            }
        }

        // tutti i vivi entro il raggio, una volta sola anche con più collider, ognuno con il suo tiro
        private void HitAround(Vector3 center, float radius, float percent, float stun, Health skip)
        {
            Collect(center, radius);
            foreach (var target in _struck)
            {
                if (target != skip)
                {
                    HitOne(target, percent, stun);
                }
            }
        }

        private void StunAround(Vector3 center, float radius, float stun)
        {
            Collect(center, radius);
            foreach (var target in _struck)
            {
                if (target.TryGetComponent(out EnemyAI enemy))
                {
                    TryStun(enemy, stun);
                }
            }
        }

        private void Collect(Vector3 center, float radius)
        {
            _struck.Clear();
            _areaTargets.Clear();
            if (radius <= 0f)
            {
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(center + Vector3.up * _searchHeight, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var target = AreaHits[i].GetComponentInParent<Health>();
                if (target != null && !target.IsDead && target.gameObject != gameObject && _areaTargets.Add(target))
                {
                    _struck.Add(target);
                }
            }

            _areaTargets.Clear();
        }

        private void TryStun(EnemyAI enemy, float seconds)
        {
            if (!enemy.Stun(seconds))
            {
                return;
            }

            float taken = enemy.StunRemaining;
            Stunned?.Invoke(enemy, taken);
            Play(_stunClip);
            if (_stunEffect != null && taken > 0f)
            {
                var stars = Instantiate(_stunEffect, enemy.transform);
                stars.transform.localPosition = Vector3.up * _stunEffectHeight;
                Destroy(stars, taken);
            }
        }

        // il nemico sotto il cursore, poi il più vicino al punto, poi il più vicino al cavaliere: a portata
        private Health PickInReach(Vector3 point)
        {
            var hovered = _controller != null ? _controller.HoveredEnemy : null;
            if (hovered != null && !hovered.IsDead && EdgeDistance(hovered) <= Reach)
            {
                return hovered;
            }

            var near = Nearest(point, _pickRadius, null);
            if (near != null && EdgeDistance(near) <= Reach)
            {
                return near;
            }

            near = Nearest(transform.position, Reach + 1.5f, null);
            return near != null && EdgeDistance(near) <= Reach ? near : null;
        }

        private Health PickForCharge(SpellDefinition spell, Vector3 point)
        {
            var hovered = _controller != null ? _controller.HoveredEnemy : null;
            var target = hovered != null && !hovered.IsDead ? hovered : Nearest(point, _pickRadius, null);
            if (target == null)
            {
                return null;
            }

            float distance = FlatDistance(transform.position, target.transform.position);
            if (distance < spell.MinRange || distance > spell.Range)
            {
                return null;
            }

            // dritto e senza muri in mezzo: la Carica non gira attorno agli angoli
            return _agent.enabled && _agent.isOnNavMesh && NavMesh.Raycast(transform.position, target.transform.position, out _, NavMesh.AllAreas) ? null : target;
        }

        // fino alla gittata del Salto verso il punto, e non oltre un muro
        private Vector3 LeapLanding(SpellDefinition spell, Vector3 point)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.magnitude > spell.Range)
            {
                point = transform.position + flat.normalized * spell.Range;
            }

            if (!NavMesh.SamplePosition(point, out var sample, 1.5f, NavMesh.AllAreas))
            {
                return transform.position;
            }

            return NavMesh.Raycast(transform.position, sample.position, out var hit, NavMesh.AllAreas) ? hit.position : sample.position;
        }

        private Health Nearest(Vector3 center, float radius, Health skip)
        {
            Collect(center, radius);
            Health best = null;
            float bestDistance = float.MaxValue;
            foreach (var target in _struck)
            {
                float distance = FlatDistance(center, target.transform.position);
                if (target != skip && target.GetComponent<EnemyAI>() != null && distance < bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // la portata di un colpo d'abilità: l'arma più il passo del gesto
        private float Reach => _attack.Weapon.Range + _reachBonus;

        private float EdgeDistance(Health target)
        {
            float radius = target.TryGetComponent(out NavMeshAgent agent) ? agent.radius : 0f;
            return FlatDistance(transform.position, target.transform.position) - _agent.radius - radius;
        }

        // fermo, e il colpo normale annullato: il cavaliere fa il gesto dell'abilità
        private void StopAndCommit(float seconds)
        {
            _attack.Interrupt(seconds);
            if (_motor != null)
            {
                _motor.Stop();
            }
        }

        private void Face(Vector3 point)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flat);
            }
        }

        private void PlayState(string state)
        {
            if (_animator != null)
            {
                _animator.PlayAbility(state);
            }
        }

        private void Play(AudioClip clip)
        {
            if (_audio != null && clip != null)
            {
                _audio.PlayOneShot(clip);
            }
        }

        private void ShowArea(AreaEffect prefab, Vector3 center, float radius)
        {
            if (prefab == null || radius <= 0f)
            {
                return;
            }

            if (!_effects.TryGetValue(prefab, out var effect) || effect == null)
            {
                // fuori dal cavaliere: il cerchio resta dove è nato anche se lui si muove
                effect = Instantiate(prefab);
                _effects[prefab] = effect;
            }

            effect.Play(center, radius);
        }

        private void OnDestroy()
        {
            foreach (var effect in _effects.Values)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
