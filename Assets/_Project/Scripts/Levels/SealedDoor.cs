using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La porta di pietra della stanza sigillata (D5 della M25): chiude un corridoio di una cella. Finché è chiusa blocca il passo (un ostacolo che
    /// ritaglia il NavMesh), la vista dei nemici e gli incantesimi (il collider sul layer degli ostacoli), e le rune viola brillano; quando la
    /// leva la apre scende nel pavimento. Una volta sola.
    /// </summary>
    [DisallowMultipleComponent]
    public class SealedDoor : MonoBehaviour
    {
        [Tooltip("Il modello della porta: scende nel pavimento quando si apre.")]
        [SerializeField] private Transform _leaf;

        [Tooltip("Le rune che brillano a porta chiusa: si spengono quando si apre.")]
        [SerializeField] private GameObject _runes;

        [SerializeField] private Light _glow;

        [Tooltip("I collider che bloccano vista e incantesimi: si spengono quando si apre.")]
        [SerializeField] private Collider[] _blockers;

        [Tooltip("Ritaglia il NavMesh nel corridoio finché la porta è chiusa.")]
        [SerializeField] private NavMeshObstacle _obstacle;

        [SerializeField, Min(0.5f)] private float _sinkDepth = 4.2f;

        [SerializeField, Min(0.1f)] private float _openDuration = 1.6f;

        private Vector3 _closedPosition;

        /// <summary>Si è aperta: non si richiude.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Ha cominciato ad aprirsi (la leva è stata tirata).</summary>
        public event Action Opened;

        private void Awake()
        {
            if (_leaf != null)
            {
                _closedPosition = _leaf.localPosition;
            }
        }

        /// <summary>Apre la porta: le rune si spengono, il passo si libera e il modello scende nel pavimento.</summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            Release();
            if (_leaf != null)
            {
                StartCoroutine(Sink());
            }

            Opened?.Invoke();
        }

        /// <summary>Già aperta in una visita di prima (ADR-079): subito spalancata, senza movimento.</summary>
        public void RestoreOpen()
        {
            IsOpen = true;
            Release();
            if (_leaf != null)
            {
                _leaf.gameObject.SetActive(false);
            }
        }

        // via ostacolo, collider, rune e luce: il passo è libero
        private void Release()
        {
            if (_obstacle != null)
            {
                _obstacle.enabled = false;
            }

            if (_blockers != null)
            {
                foreach (var blocker in _blockers)
                {
                    if (blocker != null)
                    {
                        blocker.enabled = false;
                    }
                }
            }

            if (_runes != null)
            {
                _runes.SetActive(false);
            }

            if (_glow != null)
            {
                _glow.enabled = false;
            }
        }

        private IEnumerator Sink()
        {
            Vector3 down = _closedPosition + Vector3.down * _sinkDepth;
            for (float t = 0f; t < _openDuration; t += Time.deltaTime)
            {
                _leaf.localPosition = Vector3.Lerp(_closedPosition, down, Mathf.SmoothStep(0f, 1f, t / _openDuration));
                yield return null;
            }

            _leaf.gameObject.SetActive(false);
        }
    }
}
