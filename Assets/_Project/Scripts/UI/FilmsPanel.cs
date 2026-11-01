using System;
using DarkDescent.Films;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'elenco dei filmati da rivedere, dal menu iniziale (D18 della M19): <i>L'ultima candela</i> sempre,
    /// <i>La vittoria</i> solo dopo averla vista una volta in una partita qualsiasi. Che cosa è stato visto
    /// lo ricordano le impostazioni, non il salvataggio. Sceglie e basta: il filmato lo fa partire il
    /// CompositionRoot, ascoltando <see cref="Chosen"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class FilmsPanel : MonoBehaviour
    {
        /// <summary>La preferenza accesa quando la vittoria è stata vista in una partita.</summary>
        public const string VictorySeenPreference = "films.victory_seen";

        [Tooltip("L'elenco, a tutto schermo; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [SerializeField] private TMP_Text _title;

        [SerializeField] private Button _opening;

        [SerializeField] private Button _victory;

        [SerializeField] private Button _backButton;

        /// <summary>Un filmato scelto: l'id di <see cref="FilmLibrary"/>, l'apertura o il primo pezzo della vittoria.</summary>
        public event Action<string> Chosen;

        /// <summary>Chiuso con Indietro.</summary>
        public event Action Closed;

        public bool IsOpen => _window.activeSelf;

        public Button OpeningButton => _opening;

        public Button VictoryButton => _victory;

        public Button BackButton => _backButton;

        /// <summary>La vittoria è stata vista, in una partita qualsiasi.</summary>
        public static bool VictorySeen => PlayerPrefs.GetInt(VictorySeenPreference, 0) == 1;

        /// <summary>Ricorda che la vittoria è stata vista: da qui in poi si può rivedere.</summary>
        public static void MarkVictorySeen()
        {
            PlayerPrefs.SetInt(VictorySeenPreference, 1);
            PlayerPrefs.Save();
        }

        public void Open(Localizer localizer)
        {
            _title.text = localizer.Get(TextKeys.MenuFilms);
            _victory.gameObject.SetActive(VictorySeen);
            _window.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _window.SetActive(false);
            Closed?.Invoke();
        }

        private void Awake()
        {
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            _opening.onClick.AddListener(HandleOpening);
            _victory.onClick.AddListener(HandleVictory);
            _backButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _opening.onClick.RemoveListener(HandleOpening);
            _victory.onClick.RemoveListener(HandleVictory);
            _backButton.onClick.RemoveListener(Close);
        }

        private void HandleOpening()
        {
            Chosen?.Invoke(FilmLibrary.OpeningId);
        }

        private void HandleVictory()
        {
            Chosen?.Invoke(FilmLibrary.VictoryFallId);
        }
    }
}
