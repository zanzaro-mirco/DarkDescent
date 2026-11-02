using System;
using DarkDescent.Talents;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>Come si mostra un nodo (D10 della M20): chiuso in grigio, aperto a colori, preso con il bordo d'oro.</summary>
    public enum TalentNodeState
    {
        Locked,
        Available,
        Taken,
        Full,
    }

    /// <summary>
    /// Un nodo dell'albero nel pannello: la placca nella forma del suo tipo (tonda il passivo, quadrata
    /// l'abilità attiva, ottagonale la scelta), l'anello del colore dello stato, l'icona o, finché non c'è,
    /// il nome, e i gradi presi. Non decide niente: annuncia il passaggio del mouse e i click, e il
    /// pannello risponde.
    /// </summary>
    [DisallowMultipleComponent]
    public class TalentNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _plate;
        [SerializeField] private Image _ring;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _rank;

        [Header("Colori degli stati")]
        [SerializeField] private Color _lockedColor = new Color(0.32f, 0.31f, 0.3f, 1f);
        [SerializeField] private Color _availableColor = new Color(0.55f, 0.78f, 0.95f, 1f);
        [SerializeField] private Color _takenColor = new Color(0.9f, 0.77f, 0.45f, 1f);

        [Tooltip("Quanto è scuro l'interno di un nodo chiuso.")]
        [SerializeField, Range(0f, 1f)] private float _lockedDim = 0.45f;

        /// <summary>L'albero e il nodo che mostra.</summary>
        public TalentTree Tree { get; private set; }

        public TalentNode Node { get; private set; }

        public TalentNodeState State { get; private set; }

        /// <summary>Il nome o l'icona che mostra: per i test.</summary>
        public string LabelText => _label.text;

        public string RankText => _rank.text;

        public bool ShowsIcon => _icon.enabled;

        public Color RingColor => _ring.color;

        /// <summary>Il mouse è sul nodo.</summary>
        public event Action<TalentNodeView> Hovered;

        public event Action<TalentNodeView> Unhovered;

        /// <summary>Un click sul nodo; il secondo valore è vero per il destro.</summary>
        public event Action<TalentNodeView, bool> Clicked;

        /// <summary>Mostra il nodo; l'icona vuota lascia il nome al suo posto.</summary>
        public void Show(TalentTree tree, TalentNode node, Sprite plate, Sprite ring, Sprite icon, string label, int rank, TalentNodeState state)
        {
            Tree = tree;
            Node = node;
            State = state;
            _plate.sprite = plate;
            _ring.sprite = ring;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _label.text = label;
            _label.textWrappingMode = label.IndexOf(' ') < 0 ? TMPro.TextWrappingModes.NoWrap : TMPro.TextWrappingModes.Normal;
            _label.gameObject.SetActive(icon == null);
            _rank.gameObject.SetActive(node.Ranks > 1);
            if (node.Ranks > 1)
            {
                _rank.text = $"{rank}/{node.Ranks}";
            }

            Color ringColor = state == TalentNodeState.Locked ? _lockedColor
                : state == TalentNodeState.Available ? _availableColor
                : _takenColor;
            _ring.color = ringColor;
            float dim = state == TalentNodeState.Locked ? _lockedDim : 1f;
            _plate.color = new Color(0.07f * dim + 0.03f, 0.06f * dim + 0.03f, 0.05f * dim + 0.03f, 0.96f);
            _icon.color = state == TalentNodeState.Locked ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
            _label.color = state == TalentNodeState.Locked ? new Color(0.55f, 0.53f, 0.5f, 1f) : Color.white;
            _rank.color = state == TalentNodeState.Full ? _takenColor : Color.white;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Unhovered?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
            {
                Clicked?.Invoke(this, eventData.button == PointerEventData.InputButton.Right);
            }
        }

        /// <summary>Come un click del mouse: per i test.</summary>
        public void Click(bool right = false)
        {
            Clicked?.Invoke(this, right);
        }

        public void Hover()
        {
            Hovered?.Invoke(this);
        }

        public void Unhover()
        {
            Unhovered?.Invoke(this);
        }
    }
}
