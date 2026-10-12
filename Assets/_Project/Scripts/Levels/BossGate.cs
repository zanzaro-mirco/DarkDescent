using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La porta della sala del boss (D8 della M10), il simbolo G della mappa: aperta è solo
    /// pavimento; quando comincia lo scontro le colonne di roccia salgono dal pavimento, il collider
    /// ferma i click e l'ostacolo ritaglia il NavMesh, così nessuno passa. Morto il boss, o tornato in
    /// vita il cavaliere all'ingresso, riscendono.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossGate : MonoBehaviour
    {
        [Tooltip("Le colonne: salgono da sotto il pavimento.")]
        [SerializeField] private Transform _barrier;

        [Tooltip("Il blocco della cella per il NavMesh, con il ritaglio acceso.")]
        [SerializeField] private NavMeshObstacle _obstacle;

        [Tooltip("Il collider delle colonne, sul layer degli ostacoli.")]
        [SerializeField] private Collider _collider;

        [Tooltip("Quanto sotto il pavimento stanno le colonne aperte.")]
        [SerializeField, Min(0.5f)] private float _sunkDepth = 4f;

        [SerializeField, Min(0.05f)] private float _moveDuration = 0.8f;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il rumore di roccia della porta che si chiude e si apre.")]
        [SerializeField] private AudioClip _moveClip;

        private Vector3 _raised;
        private Coroutine _moving;

        public bool IsClosed { get; private set; }

        private void Awake()
        {
            _raised = _barrier.localPosition;
            SetClosed(false);
            _barrier.localPosition = _raised + Vector3.down * _sunkDepth;
            _barrier.gameObject.SetActive(false);
        }

        public void Close()
        {
            if (IsClosed)
            {
                return;
            }

            SetClosed(true);
            _barrier.gameObject.SetActive(true);
            Move(_raised, false);
        }

        public void Open()
        {
            if (!IsClosed)
            {
                return;
            }

            SetClosed(false);
            Move(_raised + Vector3.down * _sunkDepth, true);
        }

        private void SetClosed(bool closed)
        {
            IsClosed = closed;
            _obstacle.enabled = closed;
            _collider.enabled = closed;
        }

        private void Move(Vector3 target, bool hideAtEnd)
        {
            if (_moving != null)
            {
                StopCoroutine(_moving);
            }

            if (_audio != null && _moveClip != null)
            {
                _audio.PlayOneShot(_moveClip);
            }

            _moving = StartCoroutine(Slide(target, hideAtEnd));
        }

        private IEnumerator Slide(Vector3 target, bool hideAtEnd)
        {
            Vector3 from = _barrier.localPosition;
            for (float t = 0f; t < _moveDuration; t += Time.deltaTime)
            {
                _barrier.localPosition = Vector3.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / _moveDuration));
                yield return null;
            }

            _barrier.localPosition = target;
            _barrier.gameObject.SetActive(!hideAtEnd);
            _moving = null;
        }
    }
}
