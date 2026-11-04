using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Classes;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// La creazione del personaggio (D8 della M21), dopo il posto scelto in <i>Nuova partita</i>: le classi che
    /// si possono scegliere, ognuna con la sua scheda, il modello che gira, una riga sullo stile e sui numeri
    /// di partenza, e il nome (fino a 16 lettere, uno di partenza per classe). Non scrive niente: <i>Comincia</i>
    /// dà classe e nome in <see cref="Started"/>, e <i>Indietro</i> riporta ai posti con il salvataggio
    /// intatto. Il personaggio lo fa il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCreationPanel : MonoBehaviour
    {
        // le schede delle classi: la scelta è dorata, le altre come il pulsante
        private static readonly Color Chosen = new Color(1f, 0.82f, 0.38f, 1f);

        [Tooltip("La schermata, a tutto schermo; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _window;

        [SerializeField] private TMP_Text _title;

        [Tooltip("Una scheda di classe da copiare: spenta, sotto il contenitore.")]
        [SerializeField] private Button _cardTemplate;

        [SerializeField] private CharacterPreview _preview;

        [SerializeField] private TMP_Text _description;

        [SerializeField] private TMP_Text _attributes;

        [SerializeField] private TMP_Text _nameLabel;

        [SerializeField] private TMP_InputField _name;

        [SerializeField] private TMP_Text _nameHint;

        [SerializeField] private Button _start;

        [SerializeField] private TMP_Text _startLabel;

        [SerializeField] private Button _backButton;

        private readonly List<(CharacterClass characterClass, Button button)> _cards = new List<(CharacterClass, Button)>();
        private Localizer _localizer;
        private string _autoName = "";

        /// <summary>Premuto Comincia: l'id della classe e il nome, già ripulito.</summary>
        public event Action<string, string> Started;

        /// <summary>Premuto Indietro.</summary>
        public event Action Closed;

        public bool IsOpen => _window.activeSelf;

        public CharacterClass Selected { get; private set; }

        public TMP_InputField NameField => _name;

        public Button StartButton => _start;

        public Button BackButton => _backButton;

        public CharacterPreview Preview => _preview;

        public TMP_Text Description => _description;

        public TMP_Text Attributes => _attributes;

        /// <summary>La scheda di una classe, o null.</summary>
        public Button Card(string classId) => _cards.FirstOrDefault(c => c.characterClass.Id == classId).button;

        /// <param name="hero">Il giocatore, da cui il palco copia il corpo di ogni classe.</param>
        public void Open(Localizer localizer, CharacterClassCatalog catalog, GameObject hero)
        {
            _localizer = localizer;
            var playable = catalog.Playable.ToList();
            _preview.Build(hero, playable);
            BuildCards(playable);
            _title.text = localizer.Get(TextKeys.CreateTitle);
            _nameLabel.text = localizer.Get(TextKeys.CreateName);
            _nameHint.text = localizer.Get(TextKeys.CreateNameHint);
            _startLabel.text = localizer.Get(TextKeys.CreateStart);
            _name.characterLimit = HeroName.MaxLength;
            _window.SetActive(true);
            _preview.SetRunning(true);
            Choose(playable.FirstOrDefault()?.Id);
        }

        /// <summary>La partita comincia: il palco del modello si smonta.</summary>
        public void Release()
        {
            Close();
            _preview.Release();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _preview.SetRunning(false);
            _window.SetActive(false);
        }

        /// <summary>Sceglie la classe: il modello, il testo, i numeri e, se il nome è ancora quello di partenza, il nome di lei.</summary>
        public void Choose(string classId)
        {
            var next = _cards.Select(c => c.characterClass).FirstOrDefault(c => c.Id == classId);
            if (next == null)
            {
                return;
            }

            Selected = next;
            string defaultName = _localizer.Get(next.DefaultNameKey);
            // il nome scritto da chi gioca non si tocca; quello di partenza segue la classe
            if (string.IsNullOrWhiteSpace(_name.text) || _name.text == _autoName)
            {
                _name.SetTextWithoutNotify(defaultName);
                _autoName = defaultName;
            }

            _description.text = _localizer.Get(next.DescriptionKey);
            _attributes.text = _localizer.Format(TextKeys.CreateAttributes, Mathf.RoundToInt(next.Strength),
                Mathf.RoundToInt(next.Dexterity), Mathf.RoundToInt(next.Magic), Mathf.RoundToInt(next.Vitality));
            foreach (var (characterClass, button) in _cards)
            {
                bool chosen = characterClass == next;
                button.targetGraphic.color = chosen ? Chosen : Color.white;
                if (button.TryGetComponent(out Outline outline))
                {
                    outline.enabled = chosen;
                }
                // la scelta è chiara anche dalla scritta: oro acceso, e le altre smorzate
                button.GetComponentInChildren<TMP_Text>(true).color = chosen ? new Color(1f, 0.92f, 0.6f, 1f) : new Color(0.7f, 0.64f, 0.52f, 1f);
            }

            _preview.Show(next);
        }

        private void Awake()
        {
            _window.SetActive(false);
            _cardTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _start.onClick.AddListener(HandleStart);
            _backButton.onClick.AddListener(HandleBack);
        }

        private void OnDisable()
        {
            _start.onClick.RemoveListener(HandleStart);
            _backButton.onClick.RemoveListener(HandleBack);
        }

        private void BuildCards(List<CharacterClass> playable)
        {
            foreach (var (_, button) in _cards)
            {
                Destroy(button.gameObject);
            }

            _cards.Clear();
            foreach (var characterClass in playable)
            {
                var card = Instantiate(_cardTemplate, _cardTemplate.transform.parent);
                card.gameObject.name = "Card_" + characterClass.Id;
                card.gameObject.SetActive(true);
                card.GetComponentInChildren<TMP_Text>(true).text = _localizer.Get(characterClass.NameKey);
                var emblem = card.transform.Find("Icon");
                if (emblem != null)
                {
                    emblem.GetComponent<Image>().sprite = characterClass.Icon;
                    emblem.gameObject.SetActive(characterClass.Icon != null);
                }

                string id = characterClass.Id;
                card.onClick.AddListener(() => Choose(id));
                _cards.Add((characterClass, card));
            }
        }

        private void HandleStart()
        {
            if (Selected == null)
            {
                return;
            }

            Started?.Invoke(Selected.Id, HeroName.Clean(_name.text, _localizer.Get(Selected.DefaultNameKey)));
        }

        private void HandleBack()
        {
            Close();
            Closed?.Invoke();
        }
    }
}
