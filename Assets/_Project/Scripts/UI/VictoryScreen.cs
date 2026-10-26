using System;
using System.Text;
using DarkDescent.Localization;
using DarkDescent.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La vittoria (D9 della M10): arrivati in città dal portale della vittoria, il gioco si ferma e
    /// mostra tempo di gioco, livello, nemici uccisi, morti e oro raccolto. Dalla M17 i riconoscimenti
    /// stanno nel menu iniziale (<see cref="CreditsPanel"/>): qui Continua chiude la schermata e si resta
    /// in città, Menu principale salva e torna al menu. Il salvataggio resta: con Continua del menu si
    /// riprende in città.
    /// </summary>
    [DisallowMultipleComponent]
    public class VictoryScreen : MonoBehaviour
    {
        [Tooltip("Il riquadro con le statistiche; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [SerializeField] private TMP_Text _stats;

        [SerializeField] private Button _continueButton;

        [SerializeField] private Button _menuButton;

        private Localizer _localizer;

        /// <summary>Continua: la schermata si chiude e si gioca ancora, in città.</summary>
        public event Action Continued;

        /// <summary>Menu principale: chi tiene la partita salva e torna al menu.</summary>
        public event Action Finished;

        public bool IsOpen => _window.activeSelf;

        public string StatsText => _stats.text;

        public Button ContinueButton => _continueButton;

        public Button MenuButton => _menuButton;

        public void Bind(Localizer localizer)
        {
            _localizer = localizer;
        }

        /// <summary>Ferma il gioco e mostra i contatori della partita vinta.</summary>
        /// <param name="unlockedKey">Il nome della difficoltà che si apre (D15 della M14); null se nessuna.</param>
        public void Show(RunStats stats, int level, string unlockedKey = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine(_localizer.Format(TextKeys.VictoryTime, RunStats.FormatTime(stats.PlayTime)));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryLevel, level));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryKills, stats.Kills));
            builder.AppendLine(_localizer.Format(TextKeys.VictoryDeaths, stats.Deaths));
            builder.Append(_localizer.Format(TextKeys.VictoryGold, stats.GoldCollected));
            if (!string.IsNullOrEmpty(unlockedKey))
            {
                builder.AppendLine();
                builder.AppendLine();
                builder.Append(_localizer.Format(TextKeys.VictoryUnlocked, _localizer.Get(unlockedKey)));
            }
            _stats.text = builder.ToString();
            Time.timeScale = 0f;
            _window.SetActive(true);
        }

        /// <summary>Chiude e rimette il tempo: si resta nella partita.</summary>
        public void Continue()
        {
            if (Close())
            {
                Continued?.Invoke();
            }
        }

        /// <summary>Chiude e rimette il tempo: il gioco torna al menu.</summary>
        public void Finish()
        {
            if (Close())
            {
                Finished?.Invoke();
            }
        }

        private bool Close()
        {
            if (!IsOpen)
            {
                return false;
            }

            _window.SetActive(false);
            Time.timeScale = 1f;
            return true;
        }

        private void Awake()
        {
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            _continueButton.onClick.AddListener(Continue);
            _menuButton.onClick.AddListener(Finish);
        }

        private void OnDisable()
        {
            _continueButton.onClick.RemoveListener(Continue);
            _menuButton.onClick.RemoveListener(Finish);

            // spenta a metà (cambio scena): il tempo non deve restare fermo
            if (IsOpen)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
