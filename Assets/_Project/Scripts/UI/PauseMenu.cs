using System;
using DarkDescent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La pausa con `Esc` (D10 della M10): il gioco si ferma (tempo e suoni), e si sceglie tra
    /// riprendere, le opzioni e salvare per tornare al menu iniziale. Un hit stop in corso finisce
    /// prima, altrimenti alla sua fine rimetterebbe il tempo che scorre (trappola 6).
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private Button _resume;

        [SerializeField] private Button _options;

        [SerializeField] private Button _quit;

        [SerializeField] private OptionsPanel _optionsPanel;

        [SerializeField] private HitStop _hitStop;

        private bool _paused;

        /// <summary>La pausa è cominciata (true) o finita: il cavaliere smette di ascoltare i click.</summary>
        public event Action<bool> PausedChanged;

        /// <summary>Salva ed esci: chi tiene la partita salva e ricarica la scena con il menu.</summary>
        public event Action QuitRequested;

        public bool IsOpen => _paused;

        public Button ResumeButton => _resume;

        public Button QuitButton => _quit;

        public void Open()
        {
            if (_paused)
            {
                return;
            }

            _hitStop.Cancel();
            _paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            _root.SetActive(true);
            PausedChanged?.Invoke(true);
        }

        public void Close()
        {
            if (!_paused)
            {
                return;
            }

            _paused = false;
            _optionsPanel.Closed -= HandleOptionsClosed;
            _optionsPanel.Close();
            _root.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            PausedChanged?.Invoke(false);
        }

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _resume.onClick.AddListener(Close);
            _options.onClick.AddListener(HandleOptions);
            _quit.onClick.AddListener(HandleQuit);
        }

        private void OnDisable()
        {
            _resume.onClick.RemoveListener(Close);
            _options.onClick.RemoveListener(HandleOptions);
            _quit.onClick.RemoveListener(HandleQuit);
            _optionsPanel.Closed -= HandleOptionsClosed;

            // spenta a metà (cambio scena): tempo e suoni non devono restare fermi
            if (_paused)
            {
                _paused = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        private void HandleOptions()
        {
            _root.SetActive(false);
            _optionsPanel.Closed += HandleOptionsClosed;
            _optionsPanel.Open();
        }

        private void HandleOptionsClosed()
        {
            _optionsPanel.Closed -= HandleOptionsClosed;
            if (_paused)
            {
                _root.SetActive(true);
            }
        }

        private void HandleQuit()
        {
            QuitRequested?.Invoke();
        }
    }
}
