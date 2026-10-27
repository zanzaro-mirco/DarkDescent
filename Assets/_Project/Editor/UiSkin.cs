using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La pelle dell'interfaccia (M17, D4): le immagini e i colori con cui <see cref="UiSkinTools"/>
    /// riveste finestre, tooltip e pulsanti della scena Core, nello stile scuro e pulito scelto da
    /// Mirco. Vive solo nell'editor: nel gioco restano le immagini messe sui componenti.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/UI Skin", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [SerializeField] private Sprite _panelBack;
        [SerializeField] private Sprite _panelFrame;
        [SerializeField] private Sprite _buttonPlate;

        [Tooltip("Il colore delle finestre sopra lo sfondo: quasi nero, appena trasparente.")]
        [SerializeField] private Color _windowTint = new Color(0.54f, 0.52f, 0.5f, 0.97f);

        [SerializeField] private Color _tooltipTint = new Color(0.55f, 0.53f, 0.52f, 0.96f);

        [Tooltip("Quanto si rimpicciolisce la cornice delle finestre: 1,6 fa i lati di circa 19 pixel e gli angoli di 47 a 1080p.")]
        [SerializeField, Min(0.5f)] private float _windowFrameScale = 1.6f;

        [SerializeField, Min(0.5f)] private float _tooltipFrameScale = 2.6f;

        [SerializeField, Min(0.5f)] private float _buttonScale = 1.5f;

        [Tooltip("I pulsanti: scuri a riposo, più chiari e caldi al passaggio del mouse.")]
        [SerializeField] private Color _buttonNormal = new Color(0.62f, 0.6f, 0.58f, 1f);

        [SerializeField] private Color _buttonHighlighted = new Color(1f, 0.9f, 0.7f, 1f);

        [SerializeField] private Color _buttonPressed = new Color(0.45f, 0.42f, 0.38f, 1f);

        [SerializeField] private Color _buttonDisabled = new Color(0.35f, 0.35f, 0.35f, 0.6f);

        [Header("Sfondi a tutto schermo (D6)")]
        [SerializeField] private Sprite _menuBackground;
        [SerializeField] private Sprite _deathBackground;
        [SerializeField] private Sprite _victoryBackground;

        public Sprite PanelBack => _panelBack;

        public Sprite PanelFrame => _panelFrame;

        public Sprite ButtonPlate => _buttonPlate;

        public Color WindowTint => _windowTint;

        public Color TooltipTint => _tooltipTint;

        public float WindowFrameScale => _windowFrameScale;

        public float TooltipFrameScale => _tooltipFrameScale;

        public float ButtonScale => _buttonScale;

        public Color ButtonNormal => _buttonNormal;

        public Color ButtonHighlighted => _buttonHighlighted;

        public Color ButtonPressed => _buttonPressed;

        public Color ButtonDisabled => _buttonDisabled;

        public Sprite MenuBackground => _menuBackground;

        public Sprite DeathBackground => _deathBackground;

        public Sprite VictoryBackground => _victoryBackground;

        /// <summary>Per lo strumento che crea l'asset: le tre immagini importate.</summary>
        public void SetSprites(Sprite panelBack, Sprite panelFrame, Sprite buttonPlate)
        {
            _panelBack = panelBack;
            _panelFrame = panelFrame;
            _buttonPlate = buttonPlate;
        }

        /// <summary>Per lo strumento che riveste: gli sfondi di menu, morte e vittoria importati.</summary>
        public void SetBackgrounds(Sprite menu, Sprite death, Sprite victory)
        {
            _menuBackground = menu;
            _deathBackground = death;
            _victoryBackground = victory;
        }
    }
}
