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

        [Tooltip("«Comandi»: apre il pannello dei comandi (D14 della M23).")]
        [SerializeField] private Button _help;

        [SerializeField] private HelpPanel _helpPanel;

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

        public Button HelpButton => _help;

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
            DetachHelp();
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
            if (_help != null)
            {
                _help.onClick.AddListener(HandleHelp);
            }

            _quit.onClick.AddListener(HandleQuit);
        }

        private void OnDisable()
        {
            _resume.onClick.RemoveListener(Close);
            _options.onClick.RemoveListener(HandleOptions);
            if (_help != null)
            {
                _help.onClick.RemoveListener(HandleHelp);
            }

            _quit.onClick.RemoveListener(HandleQuit);
            _optionsPanel.Closed -= HandleOptionsClosed;
            DetachHelp();

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

        /// <summary>F1 mentre si è in pausa: il pannello dei comandi passa dal menu (che si nasconde), come dal pulsante.</summary>
        public void ShowHelp()
        {
            if (_paused && _root.activeSelf)
            {
                HandleHelp();
            }
        }

        // «Comandi»: il menu si nasconde e il pannello gli sta davanti; chiuso, il menu torna (come per le opzioni)
        private void HandleHelp()
        {
            if (_helpPanel == null)
            {
                return;
            }

            _root.SetActive(false);
            _helpPanel.Closed += HandleHelpClosed;
            _helpPanel.Open();
        }

        private void HandleHelpClosed()
        {
            DetachHelp();
            if (_paused)
            {
                _root.SetActive(true);
            }
        }

        private void DetachHelp()
        {
            if (_helpPanel != null)
            {
                _helpPanel.Closed -= HandleHelpClosed;
            }
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
