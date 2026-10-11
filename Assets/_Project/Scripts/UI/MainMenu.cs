using System;
using System.Globalization;
using DarkDescent.Localization;
using DarkDescent.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il menu iniziale (D2 della M10): Continua riprende l'ultima partita giocata e manca senza
    /// salvataggi; Nuova partita sceglie uno dei tre posti, chiedendo conferma sopra uno occupato;
    /// Carica mostra i tre posti, che si possono anche cancellare; poi Opzioni ed Esci. Sceglie e
    /// basta: la partita la fa partire il CompositionRoot, ascoltando <see cref="Chosen"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Tutto il menu, con il fondo scuro che copre il gioco; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private GameObject _mainPanel;

        [SerializeField] private GameObject _slotsPanel;

        [SerializeField] private Button _continue;

        [SerializeField] private Button _newGame;

        [SerializeField] private Button _load;

        [SerializeField] private Button _options;

        [SerializeField] private Button _quit;

        [SerializeField] private TMP_Text _slotsTitle;

        [SerializeField] private SaveSlotView[] _slots = new SaveSlotView[SaveSlots.Count];

        [SerializeField] private Button _slotsBack;

        [SerializeField] private ConfirmDialog _confirm;

        [SerializeField] private OptionsPanel _optionsPanel;

        private SaveSlots _saves;
        private Localizer _localizer;
        private bool _choosingNew;

        /// <summary>Una partita scelta: il posto e se è nuova (true) o da riprendere.</summary>
        public event Action<int, bool> Chosen;

        public bool IsShown => _root.activeSelf;

        public bool IsChoosingSlot => _slotsPanel.activeSelf;

        public Button ContinueButton => _continue;

        public Button NewGameButton => _newGame;

        public Button LoadButton => _load;

        public ConfirmDialog Confirm => _confirm;

        public SaveSlotView SlotView(int slot) => _slots[slot - 1];

        public void Show(SaveSlots saves, Localizer localizer)
        {
            _saves = saves;
            _localizer = localizer;
            _root.SetActive(true);
            ShowMain();
        }

        public void Hide()
        {
            _confirm.Close();
            _optionsPanel.Closed -= ShowMain;
            _optionsPanel.Close();
            _root.SetActive(false);
        }

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _continue.onClick.AddListener(HandleContinue);
            _newGame.onClick.AddListener(HandleNewGame);
            _load.onClick.AddListener(HandleLoad);
            _options.onClick.AddListener(HandleOptions);
            _quit.onClick.AddListener(HandleQuit);
            _slotsBack.onClick.AddListener(ShowMain);
            foreach (var view in _slots)
            {
                view.Clicked += HandleSlotClicked;
                view.DeleteClicked += HandleDeleteClicked;
            }
        }

        private void OnDisable()
        {
            _continue.onClick.RemoveListener(HandleContinue);
            _newGame.onClick.RemoveListener(HandleNewGame);
            _load.onClick.RemoveListener(HandleLoad);
            _options.onClick.RemoveListener(HandleOptions);
            _quit.onClick.RemoveListener(HandleQuit);
            _slotsBack.onClick.RemoveListener(ShowMain);
            _optionsPanel.Closed -= ShowMain;
            foreach (var view in _slots)
            {
                view.Clicked -= HandleSlotClicked;
                view.DeleteClicked -= HandleDeleteClicked;
            }
        }

        private void ShowMain()
        {
            _optionsPanel.Closed -= ShowMain;
            _slotsPanel.SetActive(false);
            _mainPanel.SetActive(true);
            _continue.gameObject.SetActive(_saves.LastPlayed() > 0);
        }

        private void HandleContinue()
        {
            int slot = _saves.LastPlayed();
            if (slot > 0)
            {
                Chosen?.Invoke(slot, false);
            }
        }

        private void HandleNewGame()
        {
            ShowSlots(true);
        }

        private void HandleLoad()
        {
            ShowSlots(false);
        }

        private void HandleOptions()
        {
            _mainPanel.SetActive(false);
            _optionsPanel.Closed += ShowMain;
            _optionsPanel.Open();
        }

        private void HandleQuit()
        {
            Application.Quit();
        }

        private void ShowSlots(bool choosingNew)
        {
            _choosingNew = choosingNew;
            _mainPanel.SetActive(false);
            _slotsPanel.SetActive(true);
            _slotsTitle.text = _localizer.Get(choosingNew ? TextKeys.MenuNewTitle : TextKeys.MenuLoadTitle);
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            foreach (var view in _slots)
            {
                bool readable = _saves.TryRead(view.Slot, out SaveData data);
                bool used = _saves.IsUsed(view.Slot);
                view.Show(readable ? Summary(view.Slot, data) : _localizer.Format(TextKeys.SlotEmpty, view.Slot),
                    _choosingNew || readable, used);
            }
        }

        // "Posto 2 – livello 9 · Caverne – Livello 6 · 12/10/2026 14:30"
        private string Summary(int slot, SaveData data)
        {
            string place = data.InTown
                ? _localizer.Get(TextKeys.LevelTown)
                : _localizer.Format(TextKeys.LevelTitle, _localizer.Get(data.Scene == "Level_Caves" ? TextKeys.LevelCaves : TextKeys.LevelCrypt), data.Depth);
            DateTime? written = _saves.LastWrite(slot);
            string date = written.HasValue ? written.Value.ToLocalTime().ToString("g", Culture()) : string.Empty;
            return _localizer.Format(TextKeys.SlotSummary, slot, data.Level, place) + " · " + date;
        }

        private CultureInfo Culture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(_localizer.Language);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private void HandleSlotClicked(int slot)
        {
            if (!_choosingNew)
            {
                if (_saves.TryRead(slot, out _))
                {
                    Chosen?.Invoke(slot, false);
                }

                return;
            }

            if (_saves.IsUsed(slot))
            {
                _confirm.Ask(_localizer.Format(TextKeys.ConfirmOverwrite, slot), () => Chosen?.Invoke(slot, true));
                return;
            }

            Chosen?.Invoke(slot, true);
        }

        private void HandleDeleteClicked(int slot)
        {
            _confirm.Ask(_localizer.Format(TextKeys.ConfirmDelete, slot), () =>
            {
                _saves.Delete(slot);
                RefreshSlots();
            });
        }
    }
}
