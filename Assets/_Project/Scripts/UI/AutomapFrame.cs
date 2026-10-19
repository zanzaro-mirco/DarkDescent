using System.Collections.Generic;
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
    /// centro. Nell'angolo una maschera taglia quello che esce dalla cornice. I punti dei nemici
    /// speciali (D9 della M12) sono copie del punto del cavaliere, più piccole e sotto di lui.
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

        [Tooltip("Quanto è grande il punto di un nemico speciale rispetto a quello del cavaliere.")]
        [SerializeField, Range(0.2f, 1f)] private float _markerScale = 0.7f;

        // i punti dei nemici speciali, fatti una volta e riusati: per frame si spostano soltanto
        private readonly List<Image> _markers = new List<Image>();

        private int _width;
        private int _height;

        public bool IsShowing => gameObject.activeSelf;

        public RawImage MapImage => _map;

        public RectTransform Dot => _dot;

        /// <summary>Quanti punti di nemici speciali sono accesi, per i test.</summary>
        public int VisibleMarkers
        {
            get
            {
                int count = 0;
                foreach (var marker in _markers)
                {
                    count += marker != null && marker.gameObject.activeSelf ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>Il punto acceso numero <paramref name="index"/>, per i test.</summary>
        public Image Marker(int index)
        {
            return _markers[index];
        }

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

        /// <summary>
        /// Accende il punto di un nemico speciale, in celle continue come <see cref="Place"/>, nel colore
        /// del suo grado. Il primo uso di un indice crea il punto; dopo si riusa.
        /// </summary>
        public void PlaceMarker(int index, Vector2 cell, Color color)
        {
            while (_markers.Count <= index)
            {
                var marker = Instantiate(_dot.gameObject, _dot.parent, false);
                marker.name = "Special" + _markers.Count;
                marker.transform.localScale = Vector3.one * _markerScale;

                // sotto il punto del cavaliere, che resta sempre visibile
                marker.transform.SetSiblingIndex(_dot.GetSiblingIndex());
                _markers.Add(marker.GetComponent<Image>());
            }

            var image = _markers[index];
            image.rectTransform.anchoredPosition = ToLocal(cell);
            image.color = color;
            image.gameObject.SetActive(true);
        }

        /// <summary>Spegne i punti dei nemici speciali da <paramref name="first"/> in poi.</summary>
        public void HideMarkers(int first)
        {
            for (int i = first; i < _markers.Count; i++)
            {
                // con la scena che si scarica il punto può essere già distrutto
                if (_markers[i] != null && _markers[i].gameObject.activeSelf)
                {
                    _markers[i].gameObject.SetActive(false);
                }
            }
        }

        private Vector2 ToLocal(Vector2 cell)
        {
            return new Vector2((cell.x + 0.5f - _width * 0.5f) * _cellSize, (_height * 0.5f - cell.y - 0.5f) * _cellSize);
        }

        /// <summary>Mette il punto sul cavaliere, in celle continue: x verso est, y verso sud, i centri sugli interi.</summary>
        public void Place(Vector2 cell)
        {
            var local = ToLocal(cell);
            _dot.anchoredPosition = local;
            if (_followPlayer)
            {
                _map.rectTransform.anchoredPosition = -local;
            }
        }
    }
}
