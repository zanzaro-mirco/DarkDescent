using System;
using DarkDescent.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il nome di un oggetto a terra mostrato con `Alt` (D1 della M10): fondo scuro, testo nel colore
    /// della rarità e, se l'etichetta è salita per non coprirne un'altra, un trattino fino all'oggetto.
    /// Lo dispone <see cref="GroundLabels"/>, che le prende da un pool.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundLabel : MonoBehaviour
    {
        [SerializeField] private Button _button;

        [SerializeField] private TMP_Text _text;

        [Tooltip("Il trattino verso l'oggetto, ancorato al centro in basso: si allunga verso il basso.")]
        [SerializeField] private RectTransform _line;

        [Tooltip("Spazio orizzontale tra il testo e il bordo del fondo, per lato.")]
        [SerializeField, Min(0f)] private float _padding = 6f;

        private RectTransform _rect;

        /// <summary>Click sul nome: chi dispone le etichette manda il cavaliere a raccogliere.</summary>
        public event Action<GroundLabel> Clicked;

        public GroundItem Item { get; private set; }

        /// <summary>L'ultimo giro di disposizione in cui l'oggetto c'era ancora.</summary>
        public int Stamp { get; set; }

        public float Width => _rect.sizeDelta.x;

        public float Height => _rect.sizeDelta.y;

        public string Text => _text.text;

        public RectTransform Rect => _rect;

        /// <summary>Lega l'etichetta a un oggetto e ne scrive il nome; la larghezza segue il testo.</summary>
        public void Assign(GroundItem item, string label)
        {
            Item = item;
            _text.SetText(label);
            float width = _text.GetPreferredValues(label).x + _padding * 2f;
            _rect.sizeDelta = new Vector2(width, _rect.sizeDelta.y);
        }

        public void Release()
        {
            Item = null;
            gameObject.SetActive(false);
        }

        /// <summary>Mette il bordo basso dell'etichetta in <paramref name="bottomCenter"/>, con il trattino lungo <paramref name="lift"/>.</summary>
        public void Place(Vector2 bottomCenter, float lift)
        {
            _rect.anchoredPosition = bottomCenter;
            bool showLine = lift > 0.5f;
            if (_line.gameObject.activeSelf != showLine)
            {
                _line.gameObject.SetActive(showLine);
            }

            if (showLine)
            {
                _line.sizeDelta = new Vector2(_line.sizeDelta.x, lift);
            }
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }
    }
}
