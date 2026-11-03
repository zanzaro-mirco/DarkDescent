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

        /// <summary>I secondi che restano al potenziamento in corso; 0 se non ce n'è.</summary>
        public float BuffRemaining => _buff != null ? _buffRemaining : 0f;

        /// <summary>Un'abilità del Mago ha colpito un nemico (M21): chi se ne serve (la Concentrazione, la Pira) lo sente qui.</summary>
        public event Action<SpellDefinition, Health> Struck;

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
            EndBuff();
        }

        private void Update()
        {
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
                    LaunchProjectile(spell);
                    break;
                case SpellKind.Area:
                    StrikeArea(spell);
                    break;
                case SpellKind.Buff:
                    StartBuff(spell);
                    break;
            }
        }

        private void LaunchProjectile(SpellDefinition spell)
        {
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
            float damage = RollDamage(spell);

            // la Palla di fuoco fa tutto il suo danno allo scoppio, a tutti quelli attorno; gli altri al bersaglio che toccano
            bool blast = _magic.AbilityValue(spell, AbilityParam.Radius) > 0f;
            var projectile = _pools.Launch(spell.Effect, from, direction, spell.Speed, spell.Range, blast ? 0f : damage, gameObject, _targetLayers);
            if (blast || spell.SlowPercent > 0f || spell.Freeze > 0f)
            {
                Action<Projectile, IDamageable> detonate = null;
                detonate = (exploded, target) =>
                {
                    exploded.Exploded -= detonate;
                    Detonate(spell, exploded.transform.position, target);
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
        private void Detonate(SpellDefinition spell, Vector3 point, IDamageable direct)
        {
            float radius = _magic.AbilityValue(spell, AbilityParam.Radius);
            if (radius > 0f)
            {
                int count = Physics.OverlapSphereNonAlloc(point, radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
                _areaTargets.Clear();
                for (int i = 0; i < count; i++)
                {
                    var target = AreaHits[i].GetComponentInParent<Health>();
                    if (target == null || target.IsDead || target.gameObject == gameObject || !_areaTargets.Add(target))
                    {
                        continue;
                    }

                    target.TakeDamage(new DamageInfo(RollDamage(spell), spell.DamageType, gameObject));
                    ApplyFrost(spell, target);
                    Struck?.Invoke(spell, target);
                }

                _areaTargets.Clear();
            }
            else if (direct is Health hit)
            {
                ApplyFrost(spell, hit);
                Struck?.Invoke(spell, hit);
            }
        }

        /// <summary>Il rallentamento e il congelamento di un'abilità su un nemico, con i nodi dell'albero; niente su chi non è un nemico.</summary>
        public void ApplyFrost(SpellDefinition spell, Health target)
        {
            if (target == null || !target.TryGetComponent(out EnemyAI enemy))
            {
                return;
            }

            if (spell.SlowPercent > 0f)
            {
                enemy.Slow(spell.SlowPercent / 100f, _magic.AbilityValue(spell, AbilityParam.Slow));
            }

            if (spell.Freeze > 0f)
            {
                enemy.Freeze(_magic.AbilityValue(spell, AbilityParam.Freeze));
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
                if (target == null || target.IsDead || target.gameObject == gameObject || !_areaTargets.Add(target))
                {
                    continue;
                }

                // la Nova di gelo non fa danno, solo gelo; l'Anello di fuoco fa danno
                if (spell.MaxDamage > 0)
                {
                    target.TakeDamage(new DamageInfo(RollDamage(spell), spell.DamageType, gameObject));
                }

                ApplyFrost(spell, target);
                Struck?.Invoke(spell, target);
            }

            _areaTargets.Clear();
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
            float bonus = _stats.Sheet.Get(StatType.DamageBonus) + _magic.AbilityValue(spell, AbilityParam.Damage);
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
