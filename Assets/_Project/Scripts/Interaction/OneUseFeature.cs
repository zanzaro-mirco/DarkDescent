using System;
using UnityEngine;

namespace DarkDescent.Interaction
{
    /// <summary>
    /// Un punto d'interesse che si usa una volta sola (D6, D7 e D10 della M13): il pozzo e il santuario.
    /// Si clicca come una cassa; il cavaliere ci va e lo usa. Usato, il cristallo si spegne, la luce pure,
    /// e il nome dice "(vuoto)": si vede ancora al passaggio del mouse, ma un altro click non fa niente.
    /// Usato in una visita di prima, torna così dai ricordi del livello (<see cref="RestoreSpent"/>).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public abstract class OneUseFeature : MonoBehaviour
    {
        [Tooltip("Il cristallo: usato, prende il materiale spento.")]
        [SerializeField] private Renderer _crystal;

        [SerializeField] private Material _spentCrystal;

        [Tooltip("La luce colorata: usato, si spegne.")]
        [SerializeField] private Light _light;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _useClip;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.9f;

        private Interactable _interactable;

        /// <summary>È stato usato, adesso o in una visita di prima.</summary>
        public event Action<OneUseFeature> Spent;

        public bool IsSpent { get; private set; }

        /// <summary>La chiave del nome; usato, la stessa con ".spent" in fondo.</summary>
        protected abstract string LabelKey { get; }

        /// <summary>Il tono del suono all'uso.</summary>
        protected virtual float UsePitch => 1f;

        /// <summary>Usato in una visita di prima: spento e vuoto, senza suono né animazione.</summary>
        public void RestoreSpent()
        {
            if (IsSpent)
            {
                return;
            }

            IsSpent = true;
            ShowSpent(animate: false);
        }

        /// <summary>Dà a <paramref name="user"/> quello che il punto d'interesse dà; false se non può riceverlo.</summary>
        protected abstract bool TryApply(GameObject user);

        /// <summary>Come cambia usato: il cristallo e la luce qui, il resto (il liquido del pozzo) in chi eredita.</summary>
        protected virtual void ShowSpent(bool animate)
        {
            if (_crystal != null && _spentCrystal != null)
            {
                _crystal.sharedMaterial = _spentCrystal;
            }

            if (_light != null)
            {
                _light.enabled = false;
            }

            _interactable.SetLabel(LabelKey + ".spent", null);
        }

        protected virtual void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.SetLabel(LabelKey, null);
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void HandleUsed(GameObject user)
        {
            if (IsSpent || user == null || !TryApply(user))
            {
                return;
            }

            IsSpent = true;
            if (_audio != null && _useClip != null)
            {
                _audio.pitch = UsePitch;
                _audio.PlayOneShot(_useClip, _volume);
            }

            ShowSpent(animate: true);
            Spent?.Invoke(this);
        }
    }
}
