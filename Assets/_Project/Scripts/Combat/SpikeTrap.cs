using System;
using UnityEngine;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Gli spuntoni (D8 della M13): a riposo 2 s, poi mezzo secondo di avviso con le punte che
    /// affiorano e un suono, poi fuori per 1 s. Chi sta sulla piastrella mentre escono è il cavaliere:
    /// solo lui si fa male, una volta per scatto, per una parte della vita massima che cresce con la
    /// profondità (<see cref="TrapRules.SpikeFraction"/>). Si attraversano a tempo; i nemici passano
    /// senza danno. Ogni trappola parte da un punto del giro preso dalla sua cella, così due trappole
    /// vicine non scattano insieme. La collega il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpikeTrap : MonoBehaviour
    {
        [Tooltip("Le punte: salgono e scendono lungo Y.")]
        [SerializeField] private Transform _spikes;

        [Tooltip("Altezza delle punte a riposo, nell'avviso e fuori, rispetto al prefab.")]
        [SerializeField] private float _restHeight = -2.05f;

        [SerializeField] private float _warningHeight = -1.75f;

        [SerializeField] private float _outHeight = -0.9f;

        [SerializeField, Min(0.1f)] private float _restDuration = 2f;

        [SerializeField, Min(0.1f)] private float _warningDuration = 0.5f;

        [SerializeField, Min(0.1f)] private float _outDuration = 1f;

        [Tooltip("Quanto ci mettono le punte a cambiare altezza, in metri al secondo.")]
        [SerializeField, Min(0.1f)] private float _speed = 14f;

        [Tooltip("Metà del lato della zona che ferisce, attorno al centro della cella.")]
        [SerializeField, Min(0.1f)] private float _halfSize = 1.25f;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _warningClip;

        [SerializeField] private AudioClip _strikeClip;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        private Health _player;
        private int _depth = 1;
        private float _time;
        private SpikePhase _phase;
        private bool _struck;

        public enum SpikePhase
        {
            Rest,
            Warning,
            Out,
        }

        /// <summary>Lo scatto ha ferito il cavaliere; l'argomento è il danno.</summary>
        public event Action<float> Struck;

        public SpikePhase Phase => _phase;

        public float Cycle => _restDuration + _warningDuration + _outDuration;

        /// <summary>Il cavaliere da ferire e la profondità del livello, che decide quanto.</summary>
        public void Bind(Health player, int depth)
        {
            _player = player;
            _depth = depth;
        }

        /// <summary>Porta il giro al punto <paramref name="time"/>, in secondi dall'inizio del riposo: per i test.</summary>
        public void SetTime(float time)
        {
            _time = time;
            _phase = PhaseAt(Mathf.Repeat(_time, Cycle));
            _struck = false;
        }

        private void Awake()
        {
            // un punto del giro dalla cella: le trappole vicine non vanno a tempo tra loro
            int cellX = Mathf.RoundToInt(transform.position.x);
            int cellZ = Mathf.RoundToInt(transform.position.z);
            uint hash = unchecked((uint)cellX * 0x9E3779B1u ^ (uint)cellZ * 0x85EBCA77u);
            _time = (hash % 1000u) / 1000f * Cycle;
            _phase = PhaseAt(Mathf.Repeat(_time, Cycle));
            if (_spikes != null)
            {
                _spikes.localPosition = new Vector3(_spikes.localPosition.x, HeightOf(_phase), _spikes.localPosition.z);
            }
        }

        private void Update()
        {
            _time += Time.deltaTime;
            var phase = PhaseAt(Mathf.Repeat(_time, Cycle));
            if (phase != _phase)
            {
                _phase = phase;
                if (phase == SpikePhase.Warning)
                {
                    Play(_warningClip);
                }
                else if (phase == SpikePhase.Out)
                {
                    _struck = false;
                    Play(_strikeClip);
                }
            }

            if (_spikes != null)
            {
                Vector3 position = _spikes.localPosition;
                position.y = Mathf.MoveTowards(position.y, HeightOf(_phase), _speed * Time.deltaTime);
                _spikes.localPosition = position;
            }

            if (_phase == SpikePhase.Out && !_struck)
            {
                TryStrike();
            }
        }

        private void TryStrike()
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            Vector3 offset = _player.transform.position - transform.position;
            if (Mathf.Abs(offset.x) > _halfSize || Mathf.Abs(offset.z) > _halfSize)
            {
                return;
            }

            _struck = true;
            float damage = _player.Max * TrapRules.SpikeFraction(_depth);
            _player.TakeDamage(new DamageInfo(damage, DamageType.Physical, gameObject));
            Struck?.Invoke(damage);
        }

        private SpikePhase PhaseAt(float time)
        {
            if (time < _restDuration)
            {
                return SpikePhase.Rest;
            }

            return time < _restDuration + _warningDuration ? SpikePhase.Warning : SpikePhase.Out;
        }

        private float HeightOf(SpikePhase phase)
        {
            switch (phase)
            {
                case SpikePhase.Warning:
                    return _warningHeight;
                case SpikePhase.Out:
                    return _outHeight;
                default:
                    return _restHeight;
            }
        }

        private void Play(AudioClip clip)
        {
            if (_audio != null && clip != null && isActiveAndEnabled)
            {
                _audio.PlayOneShot(clip, _volume);
            }
        }
    }
}
