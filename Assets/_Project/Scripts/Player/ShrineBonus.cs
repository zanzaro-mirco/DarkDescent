using System;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Stats;
using UnityEngine;

namespace DarkDescent.Player
{
    /// <summary>
    /// Il bonus di un santuario sul cavaliere (D6 della M13): uno solo alla volta, un santuario nuovo
    /// prende il posto del vecchio. Danno, armatura ed esperienza sono modificatori della scheda con
    /// questo componente come sorgente, la velocità passa da <see cref="MovementSlow"/>. Finisce dopo la
    /// durata del santuario, alla morte o spegnendo il cavaliere; non si salva. L'icona accanto alla
    /// cintura ascolta gli eventi: un avviso a ogni secondo intero, mai a ogni frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    public class ShrineBonus : MonoBehaviour
    {
        [SerializeField] private AudioSource _audio;

        [Tooltip("Il suono della fine del bonus (D14).")]
        [SerializeField] private AudioClip _endClip;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;

        private CharacterStats _stats;
        private Health _health;
        private MovementSlow _speed;
        private float _remaining;
        private int _shownSeconds;

        /// <summary>Un bonus è cominciato, anche al posto di un altro.</summary>
        public event Action<ShrineDefinition> Started;

        /// <summary>I secondi interi che restano sono cambiati.</summary>
        public event Action<int> SecondsChanged;

        /// <summary>Il bonus è finito, per il tempo, la morte o un altro santuario che l'ha preso.</summary>
        public event Action<ShrineDefinition> Ended;

        /// <summary>Il santuario del bonus di adesso; null se non ce n'è.</summary>
        public ShrineDefinition Current { get; private set; }

        public float Remaining => Current != null ? _remaining : 0f;

        /// <summary>I secondi che restano, arrotondati per eccesso: quelli dell'icona.</summary>
        public int SecondsLeft => Current != null ? Mathf.CeilToInt(_remaining) : 0;

        /// <summary>Dà il bonus del santuario per la sua durata, o per <paramref name="duration"/> se è positiva (i test).</summary>
        public void Apply(ShrineDefinition shrine, float duration = 0f)
        {
            if (shrine == null || _health.IsDead)
            {
                return;
            }

            End(playSound: false);
            Current = shrine;
            _remaining = duration > 0f ? duration : shrine.Duration;
            _shownSeconds = SecondsLeft;
            switch (shrine.Effect)
            {
                case ShrineEffect.Damage:
                    _stats.Sheet.AddModifier(new StatModifier(StatType.DamageBonus, ModifierKind.Flat, shrine.Percent, this));
                    break;
                case ShrineEffect.Armor:
                    _stats.Sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Percent, shrine.Percent, this));
                    break;
                case ShrineEffect.Experience:
                    _stats.Sheet.AddModifier(new StatModifier(StatType.ExperienceBonus, ModifierKind.Flat, shrine.Percent, this));
                    break;
                case ShrineEffect.Speed:
                    if (_speed != null)
                    {
                        _speed.SetHaste(shrine.Percent / 100f);
                    }

                    break;
            }

            Started?.Invoke(shrine);
            SecondsChanged?.Invoke(_shownSeconds);
        }

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _speed = GetComponent<MovementSlow>();
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
            End(playSound: false);
        }

        private void Update()
        {
            if (Current == null)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                End(playSound: true);
                return;
            }

            int seconds = SecondsLeft;
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                SecondsChanged?.Invoke(seconds);
            }
        }

        private void HandleDied()
        {
            End(playSound: false);
        }

        private void End(bool playSound)
        {
            if (Current == null)
            {
                return;
            }

            var ended = Current;
            Current = null;
            _remaining = 0f;
            _stats.Sheet.RemoveModifiersFrom(this);
            if (_speed != null)
            {
                _speed.SetHaste(0f);
            }

            if (playSound && _audio != null && _endClip != null)
            {
                _audio.PlayOneShot(_endClip, _volume);
            }

            Ended?.Invoke(ended);
        }
    }
}
