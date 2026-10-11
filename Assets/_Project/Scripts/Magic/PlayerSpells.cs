using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Player;
using DarkDescent.Stats;
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
    public class PlayerSpells : MonoBehaviour
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

        /// <summary>I secondi che restano al potenziamento in corso; 0 se non ce n'è.</summary>
        public float BuffRemaining => _buff != null ? _buffRemaining : 0f;

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
            float damage = spell.RollDamage(_stats.Magic, _random);
            _pools.Launch(spell.Effect, from, direction, spell.Speed, spell.Range, damage, gameObject, _targetLayers);
        }

        // tutti i nemici vivi entro il raggio, una volta sola anche con più collider, ognuno con il suo tiro
        private void StrikeArea(SpellDefinition spell)
        {
            Vector3 center = transform.position;
            if (spell.Effect != null)
            {
                AreaEffectFor(spell.Effect).Play(center, spell.Radius);
            }

            if (_random == null)
            {
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(center + Vector3.up * _launchHeight, spell.Radius, AreaHits, _targetLayers, QueryTriggerInteraction.Ignore);
            _areaTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = AreaHits[i].GetComponentInParent<Health>();
                if (target == null || target.IsDead || target.gameObject == gameObject || !_areaTargets.Add(target))
                {
                    continue;
                }

                target.TakeDamage(new DamageInfo(spell.RollDamage(_stats.Magic, _random), DamageType.Fire, gameObject));
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

        private void StartBuff(SpellDefinition spell)
        {
            EndBuff();
            _buff = spell;
            _buffRemaining = spell.Duration;
            _stats.Sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Percent, spell.ArmorPercent, spell));
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

        // alla fine del tempo, alla morte o spegnendo il cavaliere
        private void EndBuff()
        {
            if (_buff == null)
            {
                return;
            }

            _stats.Sheet.RemoveModifiersFrom(_buff);
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
