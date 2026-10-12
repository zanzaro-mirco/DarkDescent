using System;
using System.Collections;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// Un portale della pergamena del ritorno (D6 della M10): un ovale azzurro che gira, con una luce
    /// e un ronzio. Raggiunto con un click, annuncia che il cavaliere ci è entrato: dove porta lo
    /// decide <see cref="TownPortals"/>. L'ovale guarda sempre la camera, anche quando la camera gira.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Portal : MonoBehaviour
    {
        [Tooltip("L'ovale: si gira verso la camera e cresce all'apertura.")]
        [SerializeField] private Transform _gate;

        [Tooltip("Il vortice dentro l'ovale, che gira attorno al suo asse.")]
        [SerializeField] private Transform _swirl;

        [Tooltip("Gradi al secondo del vortice.")]
        [SerializeField] private float _spinSpeed = 90f;

        [Tooltip("Secondi per aprirsi del tutto.")]
        [SerializeField, Min(0.05f)] private float _openDuration = 0.6f;

        [Tooltip("La sorgente del portale: il ronzio in loop.")]
        [SerializeField] private AudioSource _audio;

        [Tooltip("Il suono dell'apertura.")]
        [SerializeField] private AudioClip _openClip;

        private Interactable _interactable;
        private Transform _viewer;
        private Vector3 _gateScale;

        /// <summary>Il cavaliere l'ha raggiunto.</summary>
        public event Action<Portal> Entered;

        public Interactable Interactable => _interactable;

        /// <summary>Vero finché cresce, dopo <see cref="Open"/>.</summary>
        public bool IsOpening { get; private set; }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _gateScale = _gate.localScale;
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        /// <summary>
        /// La meta scritta sopra e la camera da guardare. <paramref name="animate"/> lo fa crescere con
        /// il suono dell'apertura: la pergamena appena letta; quello che aspetta in città c'è già.
        /// </summary>
        public void Open(string labelKey, string labelArgument, Transform viewer, bool animate)
        {
            _interactable.SetLabel(labelKey, labelArgument);
            _viewer = viewer;
            FaceViewer();
            if (animate)
            {
                StartCoroutine(Grow());
            }
        }

        private IEnumerator Grow()
        {
            IsOpening = true;
            if (_audio != null && _openClip != null)
            {
                _audio.PlayOneShot(_openClip);
            }

            for (float t = 0f; t < _openDuration; t += Time.deltaTime)
            {
                _gate.localScale = _gateScale * Mathf.SmoothStep(0.05f, 1f, t / _openDuration);
                yield return null;
            }

            _gate.localScale = _gateScale;
            IsOpening = false;
        }

        private void Update()
        {
            _swirl.Rotate(0f, 0f, _spinSpeed * Time.deltaTime, Space.Self);
            FaceViewer();
        }

        // solo il giro attorno alla verticale: l'ovale resta dritto
        private void FaceViewer()
        {
            if (_viewer == null)
            {
                return;
            }

            Vector3 toViewer = _viewer.position - _gate.position;
            toViewer.y = 0f;
            if (toViewer.sqrMagnitude > 0.0001f)
            {
                _gate.rotation = Quaternion.LookRotation(toViewer);
            }
        }

        private void HandleUsed(GameObject user)
        {
            Entered?.Invoke(this);
        }
    }
}
