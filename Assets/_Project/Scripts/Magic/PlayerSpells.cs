using System;
using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Player;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;

namespace DarkDescent.Magic
{
    /// <summary>
    /// Quello che gli incantesimi del cavaliere fanno nel mondo (M9). Al lancio il cavaliere si ferma,
    /// si gira verso il punto e fa il gesto del lancio; l'incantesimo parte quando la mano arriva
    /// avanti: il dardo vola, l'anello di fuoco colpisce tutti i nemici attorno, la pelle di pietra
    /// alza l'Armatura finché dura. Il danno viene dalla Magia, con un flusso di numeri casuali suo
    /// (trappola 3 della scheda).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMagic))]
    public class PlayerSpells : MonoBehaviour, IBoltShooter
    {
        [Tooltip("I nemici: i soli che gli incantesimi del cavaliere colpiscono.")]
        [SerializeField] private LayerMask _targetLayers;

        [Tooltip("Altezza da terra da cui parte e a cui vola il proiettile: sopra le balaustre basse.")]
        [SerializeField, Min(0f)] private float _launchHeight = 1.1f;

        [Tooltip("Distanza dal centro del cavaliere da cui parte il proiettile, davanti a lui.")]
        [SerializeField, Min(0f)] private float _launchForward = 0.5f;

        [Tooltip("Secondi tra il lancio e la partenza dell'incantesimo: il momento in cui la mano arriva avanti.")]
        [SerializeField, Min(0f)] private float _releaseDelay = 0.25f;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il soffio del lancio.")]
        [SerializeField] private AudioClip _castClip;

        // condiviso: un lancio alla volta fa la sua ricerca, nel thread principale
        private static readonly Collider[] AreaHits = new Collider[32];

        private readonly HashSet<Health> _areaTargets = new HashSet<Health>();
        private readonly Dictionary<GameObject, AreaEffect> _areaEffects = new Dictionary<GameObject, AreaEffect>();

        private PlayerMagic _magic;
        private CharacterStats _stats;
        private Health _health;
        private PlayerMotor _motor;
        private CharacterAnimatorDriver _animator;
        private ProjectilePools _pools;
        private IRandomSource _random;

        // un incantesimo che aspetta la mano: uno solo, il lancio dopo fa partire subito quello prima
        private SpellDefinition _pendingSpell;
        private Vector3 _pendingTarget;
        private float _pendingTimer;

        // il potenziamento in corso, se c'è: la sua sorgente sulla scheda è l'incantesimo stesso
        private SpellDefinition _buff;
        private float _buffRemaining;
        private AuraEffect _aura;
        private GameObject _auraPrefab;

        // il Dardo arcano del Mago, se la classe ce l'ha (M21)
        private SpellDefinition _bolt;

        // i nemici che bruciano e le aree che si ripetono (nodi dell'albero del Mago, M21)
        private readonly List<Burn> _burns = new List<Burn>();
        private readonly List<Echo> _echoes = new List<Echo>();
        private MageAbilities _mage;

        /// <summary>I secondi che restano al potenziamento in corso; 0 se non ce n'è.</summary>
        public float BuffRemaining => _buff != null ? _buffRemaining : 0f;

        /// <summary>Un'abilità del Mago ha colpito un nemico (M21): chi se ne serve (la Concentrazione, la Pira) lo sente qui.</summary>
        public event Action<SpellDefinition, Health> Struck;

        /// <summary>Un nemico si è congelato per un'abilità (M21): la Mente di ghiaccio ne ricava mana.</summary>
        public event Action<Health> Froze;

        /// <summary>Lo dice <see cref="MageAbilities"/>: la Meteora o un'area che resta ha colpito un nemico.</summary>
        public void ReportStruck(SpellDefinition spell, Health target)
        {
            Struck?.Invoke(spell, target);
        }

        /// <summary>Il dardo del sinistro di questa classe, o null (il Guerriero).</summary>
        public SpellDefinition Bolt => _bolt;

        /// <summary>Il Dardo arcano che parte quando il colpo del sinistro arriva; null per tornare al colpo dell'arma.</summary>
        public void SetBolt(SpellDefinition bolt)
        {
            _bolt = bolt;
        }

        public void Bind(ProjectilePools pools, IRandomSource random)
        {
            _pools = pools;
            _random = random;
        }

        private void Awake()
        {
            _magic = GetComponent<PlayerMagic>();
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _motor = GetComponent<PlayerMotor>();
            _animator = GetComponent<CharacterAnimatorDriver>();
            _mage = GetComponent<MageAbilities>();
        }

        private void OnEnable()
        {
            _magic.Cast += HandleCast;
            _health.Died += EndBuff;
        }

        private void OnDisable()
        {
            _magic.Cast -= HandleCast;
            _health.Died -= EndBuff;
            _pendingSpell = null;
            _burns.Clear();
            _echoes.Clear();
            EndBuff();
        }

        private void Update()
        {
            TickBurns(Time.deltaTime);
            TickEchoes(Time.deltaTime);

            if (_buff != null)
            {
                _buffRemaining -= Time.deltaTime;
                if (_buffRemaining <= 0f)
                {
                    EndBuff();
                }
            }

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
            // le abilità del Guerriero le fa WarriorAbilities, con i loro gesti (M20); quelle del Mago che non sono
            // un proiettile, un'area o un potenziamento le fa MageAbilities (M21)
            if (spell.IsWarriorAbility || spell.IsMageAbility)
            {
                return;
            }

            if (_pendingSpell != null)
            {
                Release();
            }

            if (_motor != null)
            {
                _motor.Stop();
            }

            // l'anello e la pelle di pietra non hanno una direzione: il cavaliere resta girato com'è
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (spell.Kind == SpellKind.Projectile && flat.sqrMagnitude > 0.0001f)
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

            _pendingSpell = spell;
            _pendingTarget = target;
            _pendingTimer = _releaseDelay;
        }

        private void Release()
        {
            var spell = _pendingSpell;
            _pendingSpell = null;
            switch (spell.Kind)
            {
                case SpellKind.Projectile:
                    LaunchProjectile(spell, _pendingTarget);
                    break;
                case SpellKind.Area:
                    StrikeArea(spell);
                    ScheduleEchoes(spell);
                    break;
                case SpellKind.Buff:
                    StartBuff(spell);
                    break;
            }
        }

        /// <summary>Quanti lanci gratis sono partiti: per i test.</summary>
        public int FreeCasts { get; private set; }

        /// <summary>
        /// Lancia gratis un proiettile verso un nemico (la Scintilla dell'albero, M21): senza mana, ricarica né gesto.
        /// Niente se non è un proiettile.
        /// </summary>
        public void CastFree(SpellDefinition spell, Health target)
        {
            if (spell != null && spell.Kind == SpellKind.Projectile && target != null && !target.IsDead)
            {
                FreeCasts++;
                LaunchProjectile(spell, target.transform.position);
            }
        }

        /// <summary>Vero con la probabilità data in punti percentuali, col tiro di questo flusso di numeri.</summary>
        public bool Chance(float percent)
        {
            return _random != null && percent > 0f && _random.NextDouble() * 100.0 < percent;
        }

        private void LaunchProjectile(SpellDefinition spell, Vector3 targetPoint)
        {
            if (_pools == null || spell.Effect == null)
            {
                return;
            }

            Vector3 from = transform.position + Vector3.up * _launchHeight;
            Vector3 aim = targetPoint - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
            }

            // in piano, all'altezza del petto: il dardo passa sopra le balaustre e colpisce il busto
            Vector3 direction = aim.normalized;
            from += direction * _launchForward;
            float damage = RollDamage(spell);

            // la Palla di fuoco fa tutto il suo danno allo scoppio, a tutti quelli attorno; gli altri al bersaglio che toccano
            bool blast = _magic.AbilityValue(spell, AbilityParam.Radius) > 0f;
            var projectile = _pools.Launch(spell.Effect, from, direction, spell.Speed, spell.Range, blast ? 0f : damage, gameObject, _targetLayers);
            bool burns = _magic.AbilityValue(spell, AbilityParam.Burn) > 0f;
            bool ground = _magic.AbilityValue(spell, AbilityParam.Ground) > 0f;
            if (blast || burns || ground || spell.SlowPercent > 0f || spell.Freeze > 0f)
            {
                Action<Projectile, IDamageable> detonate = null;
                detonate = (exploded, target) =>
                {
                    exploded.Exploded -= detonate;
                    Detonate(spell, exploded.transform.position, target, damage);
                };
                projectile.Exploded += detonate;
            }
        }

        /// <summary>
        /// Il Dardo arcano del sinistro (M21): parte dalla mano verso il bersaglio, con il danno che
        /// <see cref="MeleeAttack"/> ha già tirato. Con il dardo non impostato non fa niente.
        /// </summary>
        public void Shoot(Transform target, float damage)
        {
            if (_bolt == null || _bolt.Effect == null || _pools == null || target == null)
            {
                return;
            }

            Vector3 aim = target.position - transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
            }

            Vector3 direction = aim.normalized;
            Vector3 from = transform.position + Vector3.up * _launchHeight + direction * _launchForward;
            var projectile = _pools.Launch(_bolt.Effect, from, direction, _bolt.Speed, _bolt.Range, damage, gameObject, _targetLayers);
            var bolt = _bolt;
            Action<Projectile, IDamageable> arrived = null;
            arrived = (exploded, hit) =>
            {
                exploded.Exploded -= arrived;
                if (hit is Health health)
                {
                    Struck?.Invoke(bolt, health);
                }
            };
            projectile.Exploded += arrived;
        }

        // lo scoppio di un proiettile del Mago: la Palla di fuoco colpisce tutti entro il raggio, ognuno con il suo tiro,
        // il Dardo di ghiaccio rallenta chi ha colpito; il gelo e il rallentamento seguono le regole dei nemici
        private void Detonate(SpellDefinition spell, Vector3 point, IDamageable direct, float directDamage)
        {
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            float ground = _magic.AbilityValue(spell, AbilityParam.Ground);
            if (ground > 0f && _mage != null)
            {
                // la Marea di fuoco: le fiamme restano dove è scoppiata, per quanto è larga
                _mage.OpenGround(spell, point, Mathf.Max(radius, 1f), ground);
            }

            if (radius > 0f)
            {
                int count = Physics.OverlapSphereNonAlloc(point, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
                _areaTargets.Clear();
                for (int i = 0; i < count; i++)
                {
                    var target = AreaHits[i].GetComponentInParent<Health>();
                    if (target == null)
                    {
                        BreakBarrel(AreaHits[i]);
                        continue;
                    }

                    if (target.IsDead || target.gameObject == gameObject || !_areaTargets.Add(target))
                    {
                        continue;
                    }

                    float damage = RollDamage(spell);
                    target.TakeDamage(new DamageInfo(damage, spell.DamageType, gameObject));
                    ApplyFrost(spell, target);
                    ApplyBurn(spell, target, damage);
                    Struck?.Invoke(spell, target);
                }

                _areaTargets.Clear();
            }
            else if (direct is Health hit)
            {
                ApplyFrost(spell, hit);
                ApplyBurn(spell, hit, directDamage);
                Struck?.Invoke(spell, hit);
            }
        }

        /// <summary>
        /// Il fuoco che resta (Fiammata, Anello infuocato, M21): il nemico colpito brucia per i secondi del nodo, e in tutto
        /// perde il 60% del danno del colpo. Chi brucia già tiene il più forte e il più lungo.
        /// </summary>
        public void ApplyBurn(SpellDefinition spell, Health target, float hitDamage)
        {
            float seconds = _magic.AbilityValue(spell, AbilityParam.Burn);
            if (seconds <= 0f || target == null || target.IsDead || hitDamage <= 0f)
            {
                return;
            }

            float perSecond = hitDamage * BurnFraction / seconds;
            foreach (var burn in _burns)
            {
                if (burn.Target == target)
                {
                    burn.PerSecond = Mathf.Max(burn.PerSecond, perSecond);
                    burn.Remaining = Mathf.Max(burn.Remaining, seconds);
                    return;
                }
            }

            _burns.Add(new Burn { Target = target, PerSecond = perSecond, Remaining = seconds, Tick = BurnInterval });
        }

        /// <summary>Quanti nemici bruciano adesso: per i test.</summary>
        public int Burning => _burns.Count;

        private void TickBurns(float deltaTime)
        {
            for (int i = _burns.Count - 1; i >= 0; i--)
            {
                var burn = _burns[i];
                if (burn.Target == null || burn.Target.IsDead)
                {
                    _burns.RemoveAt(i);
                    continue;
                }

                // il danno si somma tempo per tempo e cade a battiti; l'ultimo porta il resto, così in tutto è quello detto
                burn.Pending += burn.PerSecond * Mathf.Min(deltaTime, burn.Remaining);
                burn.Remaining -= deltaTime;
                burn.Tick -= deltaTime;
                if (burn.Tick <= 0f || burn.Remaining <= 0f)
                {
                    burn.Tick += BurnInterval;
                    if (burn.Pending > 0f)
                    {
                        burn.Target.TakeDamage(new DamageInfo(burn.Pending, DamageType.Fire, gameObject));
                        burn.Pending = 0f;
                    }
                }

                if (burn.Remaining <= 0f)
                {
                    _burns.RemoveAt(i);
                }
            }
        }

        // la parte del colpo che resta a bruciare, e ogni quanto cade; una ripetizione arriva dopo un secondo
        public const float BurnFraction = 0.6f;
        private const float BurnInterval = 0.5f;
        public const float EchoDelay = 1f;

        private void ScheduleEchoes(SpellDefinition spell)
        {
            int extra = Mathf.RoundToInt(_magic.AbilityValue(spell, AbilityParam.Echo));
            for (int i = 1; i <= extra; i++)
            {
                _echoes.Add(new Echo { Spell = spell, Timer = EchoDelay * i });
            }
        }

        // la Nova doppia: scatta ancora attorno a dove è adesso il Mago
        private void TickEchoes(float deltaTime)
        {
            for (int i = _echoes.Count - 1; i >= 0; i--)
            {
                var echo = _echoes[i];
                echo.Timer -= deltaTime;
                if (echo.Timer <= 0f)
                {
                    _echoes.RemoveAt(i);
                    if (!_health.IsDead)
                    {
                        StrikeArea(echo.Spell);
                    }
                }
            }
        }

        private sealed class Burn
        {
            public Health Target;
            public float PerSecond;
            public float Remaining;
            public float Tick;
            public float Pending;
        }

        private sealed class Echo
        {
            public SpellDefinition Spell;
            public float Timer;
        }

        /// <summary>Il rallentamento e il congelamento di un'abilità su un nemico, con i nodi dell'albero; niente su chi non è un nemico.</summary>
        public void ApplyFrost(SpellDefinition spell, Health target)
        {
            if (target == null || !target.TryGetComponent(out EnemyAI enemy))
            {
                return;
            }

            float freeze = spell.Freeze > 0f ? _magic.AbilityValue(spell, AbilityParam.Freeze) : 0f;
            float seconds = spell.SlowPercent > 0f ? _magic.AbilityValue(spell, AbilityParam.Slow) : 0f;
            if (seconds > 0f)
            {
                // con il gelo che resta, il rallentamento dura quanto il gelo più i suoi secondi: finito il gelo, ne restano tutti
                enemy.Slow(spell.SlowPercent / 100f, seconds + freeze);
            }

            if (freeze > 0f && enemy.Freeze(freeze))
            {
                Froze?.Invoke(target);
            }
        }

        // tutti i nemici vivi entro il raggio, una volta sola anche con più collider, ognuno con il suo tiro
        private void StrikeArea(SpellDefinition spell)
        {
            Vector3 center = transform.position;
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            if (spell.Effect != null)
            {
                AreaEffectFor(spell.Effect).Play(center, radius);
            }

            if (_random == null)
            {
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(center + Vector3.up * _launchHeight, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
            _areaTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = AreaHits[i].GetComponentInParent<Health>();
                if (target == null)
                {
                    // i barili dentro l'anello si rompono, anche con la Nova di gelo che non fa danno (D11 della M25)
                    BreakBarrel(AreaHits[i]);
                    continue;
                }

                if (target.IsDead || target.gameObject == gameObject || !_areaTargets.Add(target))
                {
                    continue;
                }

                // la Nova di gelo non fa danno, solo gelo; l'Anello di fuoco fa danno
                if (spell.MaxDamage > 0)
                {
                    float damage = RollDamage(spell);
                    target.TakeDamage(new DamageInfo(damage, spell.DamageType, gameObject));
                    ApplyBurn(spell, target, damage);
                }

                ApplyFrost(spell, target);
                Struck?.Invoke(spell, target);
            }

            _areaTargets.Clear();
        }

        // un barile dentro lo scoppio o l'anello si rompe (D11 della M25), anche l'esplosivo: accende la miccia come con un colpo
        private void BreakBarrel(Collider hit)
        {
            var barrel = hit.GetComponentInParent<Breakable>();
            if (barrel != null && !barrel.IsBroken)
            {
                barrel.Break(gameObject);
            }
        }

        private AreaEffect AreaEffectFor(GameObject prefab)
        {
            if (!_areaEffects.TryGetValue(prefab, out var effect) || effect == null)
            {
                // fuori dal cavaliere: l'anello resta dove è nato anche se lui si muove
                effect = Instantiate(prefab).GetComponent<AreaEffect>();
                _areaEffects[prefab] = effect;
            }

            return effect;
        }

        // il danno dell'incantesimo con il bonus del santuario della Furia (M13) e quello dei nodi dell'albero (M21)
        public float RollDamage(SpellDefinition spell)
        {
            float bonus = _stats.Sheet.Get(StatType.DamageBonus) + _stats.Sheet.Get(StatType.SpellDamage) + _magic.AbilityValue(spell, AbilityParam.Damage);
            return CombatFormulas.WithBonus(spell.RollDamage(_stats.Magic, _random), bonus);
        }

        private void StartBuff(SpellDefinition spell)
        {
            EndBuff();
            _buff = spell;
            // con la Seconda pelle dell'albero dura di più (M20)
            _buffRemaining = _magic.AbilityValue(spell, Talents.AbilityParam.Duration);
            _stats.Sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Percent, spell.ArmorPercent, spell));
            if (spell.SlowPercent > 0f)
            {
                // l'Armatura di ghiaccio (M21): chi colpisce in mischia viene rallentato
                _health.Damaged += SlowAttacker;
            }

            if (spell.Effect == null)
            {
                return;
            }

            if (_aura == null || _auraPrefab != spell.Effect)
            {
                if (_aura != null)
                {
                    Destroy(_aura.gameObject);
                }

                // figlio del cavaliere: l'alone lo segue
                _aura = Instantiate(spell.Effect, transform).GetComponent<AuraEffect>();
                _auraPrefab = spell.Effect;
            }

            _aura.transform.localPosition = Vector3.zero;
            _aura.Play();
        }

        // chi colpisce di mischia mentre l'Armatura di ghiaccio dura: i colpi fisici da vicino, non gli incantesimi di lontano
        private void SlowAttacker(DamageInfo info, float applied)
        {
            if (_buff == null || _buff.SlowPercent <= 0f || info.Source == null || info.Type != DamageType.Physical)
            {
                return;
            }

            if ((info.Source.transform.position - transform.position).sqrMagnitude > MeleeReach * MeleeReach)
            {
                return;
            }

            if (info.Source.TryGetComponent(out EnemyAI attacker))
            {
                attacker.Slow(_buff.SlowPercent / 100f, _magic.AbilityValue(_buff, AbilityParam.Slow));
            }
        }

        // oltre questa distanza un colpo fisico non è di mischia: la trappola o il masso, non un nemico a un passo
        private const float MeleeReach = 4f;

        // alla fine del tempo, alla morte o spegnendo il cavaliere
        private void EndBuff()
        {
            if (_buff == null)
            {
                return;
            }

            _stats.Sheet.RemoveModifiersFrom(_buff);
            _health.Damaged -= SlowAttacker;
            _buff = null;
            _buffRemaining = 0f;
            if (_aura != null)
            {
                _aura.Stop();
            }
        }

        private void OnDestroy()
        {
            foreach (var effect in _areaEffects.Values)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }
        }
    }
}
