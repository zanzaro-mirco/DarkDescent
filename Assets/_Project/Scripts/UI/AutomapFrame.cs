using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Una delle due viste dell'automappa: la minimappa nell'angolo o quella sovrapposta al gioco.
    /// La mappa sta dentro un genitore ruotato come l'imbardata della camera (45° all'inizio) e uno
    /// schiacciato a metà, così è orientata come la camera isometrica: il nord della mappa cade dove
    /// cade nello schermo, anche quando la camera gira (D12 della M8). Il punto del cavaliere
    /// è figlio della mappa; con <c>_followPlayer</c> è la mappa a scorrere, e il cavaliere resta al
    /// centro. Nell'angolo una maschera taglia quello che esce dalla cornice.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutomapFrame : MonoBehaviour
    {
        [Tooltip("L'immagine della mappa, senza bersaglio dei raggi: i click devono arrivare al mondo.")]
        [SerializeField] private RawImage _map;

        [Tooltip("Il punto del cavaliere, figlio della mappa e ancorato al suo centro.")]
        [SerializeField] private RectTransform _dot;

        [Tooltip("Lato di una cella in pixel di riferimento, prima della rotazione.")]
        [SerializeField, Min(1f)] private float _cellSize = 22f;

        [Tooltip("Vero: la mappa scorre e il cavaliere resta al centro (vista sovrapposta). Falso: la mappa sta ferma e il punto si muove.")]
        [SerializeField] private bool _followPlayer;

        private int _width;
        private int _height;

        public bool IsShowing => gameObject.activeSelf;

        public RawImage MapImage => _map;

        public RectTransform Dot => _dot;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>La texture dell'automappa e quante celle ha la mappa: decidono la misura dell'immagine.</summary>
        public void Show(Texture texture, int width, int height)
        {
            _width = width;
            _height = height;
            _map.texture = texture;
            _map.rectTransform.sizeDelta = new Vector2(width, height) * _cellSize;
        }

        /// <summary>Gira la mappa con la camera: l'angolo è l'imbardata, in gradi.</summary>
        public void SetYaw(float yaw)
        {
            _map.rectTransform.parent.localRotation = Quaternion.Euler(0f, 0f, yaw);
        }

        /// <summary>Mette il punto sul cavaliere, in celle continue: x verso est, y verso sud, i centri sugli interi.</summary>
        public void Place(Vector2 cell)
        {
            var local = new Vector2((cell.x + 0.5f - _width * 0.5f) * _cellSize, (_height * 0.5f - cell.y - 0.5f) * _cellSize);
            _dot.anchoredPosition = local;
            if (_followPlayer)
            {
                _map.rectTransform.anchoredPosition = -local;
            }
        }
    }
}
