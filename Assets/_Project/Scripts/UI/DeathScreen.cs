using System;
using System.Collections;
using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Schermata di morte: compare quando il player muore, dopo il tempo dell'animazione, e offre
    /// di continuare. Non fa niente da sé: annuncia la richiesta, chi collega la scena decide.
    /// Sparisce quando il player torna in vita.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeathScreen : MonoBehaviour
    {
        [Tooltip("Il pannello da mostrare; spento all'avvio. Il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _panel;

        [SerializeField] private Button _restartButton;

        [Tooltip("Secondi tra la morte e la schermata, per lasciar vedere la caduta. In tempo reale: non risente di Time.timeScale.")]
        [SerializeField, Min(0f)] private float _showDelay = 2f;

        private Health _health;
        private bool _subscribed;
        private Coroutine _showRoutine;

        /// <summary>Premuto Continua.</summary>
        public event Action RestartRequested;

        public bool IsShown => _panel.activeSelf;

        /// <summary>Come la sfera: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(Health health)
        {
            Unsubscribe();
            _health = health;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            _restartButton.onClick.AddListener(HandleRestartClicked);
            Subscribe();
        }

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(HandleRestartClicked);
            Unsubscribe();

            // una coroutine si ferma con il componente: la schermata non deve comparire a metà
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _health == null)
            {
                return;
            }

            _health.Died += HandleDied;
            _health.Revived += HandleRevived;
            _subscribed = true;

            // morto mentre la schermata era spenta: si mostra subito
            if (_health.IsDead)
            {
                _panel.SetActive(true);
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _health.Died -= HandleDied;
            _health.Revived -= HandleRevived;
            _subscribed = false;
        }

        private void HandleDied()
        {
            _showRoutine = StartCoroutine(ShowAfterDelay());
        }

        private IEnumerator ShowAfterDelay()
        {
            // Realtime: con un hit stop in corso (timeScale a 0) WaitForSeconds non finirebbe mai
            yield return new WaitForSecondsRealtime(_showDelay);
            _panel.SetActive(true);
            _showRoutine = null;
        }

        private void HandleRevived()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            _panel.SetActive(false);
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }
    }
}
