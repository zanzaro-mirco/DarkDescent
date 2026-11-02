using System;
using System.Collections.Generic;
using DarkDescent.Localization;
using DarkDescent.Progression;
using DarkDescent.Talents;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello dell'altare (D3 della M14): le difficoltà in colonna, l'attuale segnata, quelle
    /// aperte cliccabili, le chiuse in grigio; sotto, "Rifai il mondo". Ogni scelta chiede conferma.
    /// Non cambia niente da sé: annuncia la richiesta, chi tiene la partita decide.
    /// </summary>
    [DisallowMultipleComponent]
    public class DifficultyPanel : MonoBehaviour
    {
        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [Tooltip("La riga di una difficoltà: si copia una volta per gradino, sotto di lei.")]
        [SerializeField] private Button _rowTemplate;

        [Tooltip("Di quanto scende ogni riga rispetto alla precedente.")]
        [SerializeField, Min(1f)] private float _rowStep = 56f;

        [SerializeField] private Button _newWorld;

        [Tooltip("Ricomincia l'albero delle abilità per 30 oro a livello (D11 della M20).")]
        [SerializeField] private Button _resetTalents;

        [SerializeField] private Button _close;

        [SerializeField] private ConfirmDialog _confirm;

        [Tooltip("Il colore delle difficoltà chiuse.")]
        [SerializeField] private Color _lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Tooltip("Quanto i nomi aperti vanno verso il bianco: il blu notte sul fondo scuro non si legge.")]
        [SerializeField, Range(0f, 1f)] private float _lift = 0.4f;

        private readonly List<Button> _rows = new List<Button>();
        private readonly List<TMP_Text> _labels = new List<TMP_Text>();
        private readonly List<UnityAction> _rowActions = new List<UnityAction>();
        private DifficultyTable _table;
        private DifficultyProgress _progress;
        private Localizer _localizer;
        private TalentBuild _talents;
        private bool _subscribed;

        /// <summary>Confermata un'altra difficoltà: l'indice.</summary>
        public event Action<int> SelectRequested;

        /// <summary>Confermato "Rifai il mondo".</summary>
        public event Action NewWorldRequested;

        /// <summary>Confermato "Riassegna le abilità": chi tiene la partita prende l'oro e azzera l'albero.</summary>
        public event Action ResetTalentsRequested;

        public bool IsOpen => _root.activeSelf;

        public Button NewWorldButton => _newWorld;

        public Button ResetTalentsButton => _resetTalents;

        /// <summary>Il testo della voce delle abilità: per i test.</summary>
        public string ResetTalentsText => _resetTalents.GetComponentInChildren<TMP_Text>(true).text;

        public Button CloseButton => _close;

        /// <summary>La riga della difficoltà numero <paramref name="index"/>.</summary>
        public Button Row(int index) => _rows[index];

        /// <summary>Il testo della riga numero <paramref name="index"/>.</summary>
        public string RowText(int index) => _labels[index].text;

        /// <summary>Crea le righe, una volta sola; come i pannelli, Bind e OnEnable in ordine qualsiasi.</summary>
        public void Bind(DifficultyTable table, DifficultyProgress progress, Localizer localizer)
        {
            Unsubscribe();
            _table = table;
            _progress = progress;
            _localizer = localizer;
            if (_rows.Count == 0)
            {
                CreateRows();
            }

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Collega l'albero delle abilità del cavaliere: la voce dell'altare ne legge il livello e quel che c'è da azzerare.</summary>
        public void BindTalents(TalentBuild talents)
        {
            _talents = talents;
        }

        public void Open()
        {
            Refresh();
            RefreshTalents();
            _root.SetActive(true);
        }

        public void Close()
        {
            _confirm.Close();
            _root.SetActive(false);
        }

        private void Awake()
        {
            _root.SetActive(false);
            _rowTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _newWorld.onClick.AddListener(HandleNewWorldClicked);
            _resetTalents.onClick.AddListener(HandleResetTalentsClicked);
            _close.onClick.AddListener(Close);
            Subscribe();
        }

        private void OnDisable()
        {
            _newWorld.onClick.RemoveListener(HandleNewWorldClicked);
            _resetTalents.onClick.RemoveListener(HandleResetTalentsClicked);
            _close.onClick.RemoveListener(Close);
            Unsubscribe();
        }

        private void CreateRows()
        {
            var template = (RectTransform)_rowTemplate.transform;
            for (int i = 0; i < _table.Count; i++)
            {
                var row = Instantiate(_rowTemplate, template.parent);
                row.name = $"Row{i}";
                var rect = (RectTransform)row.transform;
                rect.anchoredPosition = template.anchoredPosition + Vector2.down * (_rowStep * i);
                row.gameObject.SetActive(true);
                int index = i;
                _rows.Add(row);
                _labels.Add(row.GetComponentInChildren<TMP_Text>(true));
                _rowActions.Add(() => HandleRowClicked(index));
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _progress == null)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].onClick.AddListener(_rowActions[i]);
            }

            _progress.Changed += Refresh;
            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].onClick.RemoveListener(_rowActions[i]);
            }

            _progress.Changed -= Refresh;
            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        // "Ombra · attuale", "Crepuscolo · completata", "Tenebra · chiusa": solo all'apertura e quando cambia
        private void Refresh()
        {
            if (_progress == null)
            {
                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                var tier = _table.Tier(i);
                string name = _localizer.Get(tier.NameKey);
                bool open = _progress.CanSelect(i);
                string state = i == _progress.Current ? TextKeys.AltarCurrent
                    : i <= _progress.HighestCompleted ? TextKeys.AltarCompleted
                    : open ? null : TextKeys.AltarLocked;
                _labels[i].text = state != null ? _localizer.Format(state, name) : name;
                _labels[i].color = open ? Color.Lerp(tier.Color, Color.white, _lift) : _lockedColor;
                _rows[i].interactable = open && i != _progress.Current;
            }
        }

        private void HandleRowClicked(int index)
        {
            if (!_progress.CanSelect(index) || index == _progress.Current)
            {
                return;
            }

            string name = _localizer.Get(_table.Tier(index).NameKey);
            _confirm.Ask(_localizer.Format(TextKeys.AltarConfirmDifficulty, name), () => SelectRequested?.Invoke(index));
        }

        private void HandleNewWorldClicked()
        {
            _confirm.Ask(_localizer.Get(TextKeys.AltarConfirmNewWorld), () => NewWorldRequested?.Invoke());
        }

        // "Riassegna le abilità · 600 oro": acceso solo se c'è qualcosa di applicato da ricominciare
        private void RefreshTalents()
        {
            if (_talents == null)
            {
                return;
            }

            string text = _localizer.Format(TextKeys.AltarTalents, TalentPoints.ResetCost(_talents.Level));
            _resetTalents.GetComponentInChildren<TMP_Text>(true).text = text;
            _resetTalents.interactable = _talents.HasApplied;
        }

        private void HandleResetTalentsClicked()
        {
            if (_talents == null || !_talents.HasApplied)
            {
                return;
            }

            int cost = TalentPoints.ResetCost(_talents.Level);
            _confirm.Ask(_localizer.Format(TextKeys.AltarConfirmTalents, cost), () => ResetTalentsRequested?.Invoke());
        }
    }
}
