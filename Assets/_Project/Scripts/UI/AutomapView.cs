using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Rendering;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'automappa (D15 della scheda M6): una texture disegnata dalle celle scoperte e due viste che
    /// la mostrano, la minimappa nell'angolo e la mappa sovrapposta al gioco, tutte e due centrate
    /// sul cavaliere. Il tasto <c>M</c> scorre le modalità: angolo, sovrapposta, spenta, e da capo.
    /// Si ridisegna solo quando si scopre una cella; per frame si sposta soltanto il punto del
    /// cavaliere, e solo con una vista accesa. Insieme a lui si spostano i punti dei campioni e
    /// dell'unico (D9 della M12), nel colore del grado, quando stanno su una cella già scoperta.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutomapView : MonoBehaviour
    {
        [SerializeField] private AutomapFrame _corner;

        [SerializeField] private AutomapFrame _overlay;

        [Tooltip("Pixel della texture per lato di cella: abbastanza per disegnare i muri come linee.")]
        [SerializeField, Range(4, 32)] private int _pixelsPerCell = 20;

        private ExplorationTracker _tracker;
        private PlayerInputReader _reader;
        private CameraRotation _rotation;
        private Texture2D _texture;
        private Color32[] _pixels;
        private bool _subscribed;

        // i nemici speciali del livello e la loro vita, per spegnere il punto quando muoiono
        private readonly List<SpecialEnemy> _specials = new List<SpecialEnemy>();
        private readonly List<Health> _specialHealth = new List<Health>();

        /// <summary>La modalità scelta con <c>M</c>; si parte dalla minimappa. Resta anche passando per un livello senza mappa.</summary>
        public AutomapMode Mode { get; private set; } = AutomapMode.Corner;

        public AutomapFrame Corner => _corner;

        public AutomapFrame Overlay => _overlay;

        public Texture2D Texture => _texture;

        /// <summary>Quante volte la texture è stata ridisegnata: per i test, che controllano che non succeda a ogni frame.</summary>
        public int Repaints { get; private set; }

        /// <summary>Come la sfera: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(ExplorationTracker tracker, PlayerInputReader reader, CameraRotation rotation)
        {
            Unsubscribe();
            _tracker = tracker;
            _reader = reader;
            _rotation = rotation;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>I nemici speciali del livello: il composition root li dà quando lega il livello.</summary>
        public void SetSpecials(IEnumerable<SpecialEnemy> specials)
        {
            ClearSpecials();
            foreach (var special in specials)
            {
                if (special != null && special.IsBound)
                {
                    _specials.Add(special);
                    _specialHealth.Add(special.GetComponent<Health>());
                }
            }

            PlaceDot();
        }

        /// <summary>Il livello se ne va: niente più punti.</summary>
        public void ClearSpecials()
        {
            _specials.Clear();
            _specialHealth.Clear();
            _corner.HideMarkers(0);
            _overlay.HideMarkers(0);
        }

        /// <summary>Passa alla modalità dopo: angolo, sovrapposta, spenta, e di nuovo angolo.</summary>
        public void Cycle()
        {
            Mode = Mode == AutomapMode.Corner ? AutomapMode.Overlay : Mode == AutomapMode.Overlay ? AutomapMode.Hidden : AutomapMode.Corner;
            RefreshVisibility();
        }

        private void Awake()
        {
            _corner.SetVisible(false);
            _overlay.SetVisible(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Destroy(_texture);
        }

        private void Subscribe()
        {
            if (_subscribed || _tracker == null)
            {
                return;
            }

            _tracker.LevelChanged += HandleLevelChanged;
            _tracker.Explored += Repaint;
            _reader.MapCycled += Cycle;
            _rotation.YawChanged += Rotate;
            _subscribed = true;

            HandleLevelChanged();
            Repaint();
            Rotate(_rotation.Yaw);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _tracker.LevelChanged -= HandleLevelChanged;
            _tracker.Explored -= Repaint;
            _reader.MapCycled -= Cycle;
            _rotation.YawChanged -= Rotate;
            _subscribed = false;
        }

        // le due viste girano con la camera: la mappa resta orientata come lo schermo
        private void Rotate(float yaw)
        {
            _corner.SetYaw(yaw);
            _overlay.SetYaw(yaw);
        }

        private void HandleLevelChanged()
        {
            var exploration = _tracker.Exploration;
            if (exploration != null)
            {
                var map = exploration.Map;
                int width = map.Width * _pixelsPerCell, height = map.Height * _pixelsPerCell;

                // le cripte hanno tutte la stessa misura: la texture si rifà solo se cambia
                if (_texture == null || _texture.width != width || _texture.height != height)
                {
                    Destroy(_texture);
                    _texture = MapPainter.CreateExploredTexture(map, _pixelsPerCell);
                    _pixels = new Color32[width * height];
                }

                _corner.Show(_texture, map.Width, map.Height);
                _overlay.Show(_texture, map.Width, map.Height);
            }

            RefreshVisibility();
        }

        private void Repaint()
        {
            var exploration = _tracker.Exploration;
            if (exploration == null || _texture == null)
            {
                return;
            }

            MapPainter.PaintExplored(exploration, _texture, _pixels);
            Repaints++;
        }

        private void RefreshVisibility()
        {
            bool hasMap = _tracker != null && _tracker.Exploration != null;
            _corner.SetVisible(hasMap && Mode == AutomapMode.Corner);
            _overlay.SetVisible(hasMap && Mode == AutomapMode.Overlay);
            PlaceDot();
        }

        // seguire il cavaliere è posizione, non dati, come l'oggetto sul cursore
        private void LateUpdate()
        {
            PlaceDot();
        }

        private void PlaceDot()
        {
            AutomapFrame frame = _corner.IsShowing ? _corner : _overlay.IsShowing ? _overlay : null;
            if (frame == null || _tracker.Player == null)
            {
                return;
            }

            Vector3 position = _tracker.Player.position;
            frame.Place(ToCell(position));
            PlaceSpecials(frame);
        }

        // un punto per ogni nemico speciale vivo su una cella scoperta: visto, si sa dov'è
        private void PlaceSpecials(AutomapFrame frame)
        {
            int shown = 0;
            var exploration = _tracker.Exploration;
            for (int i = 0; i < _specials.Count && exploration != null; i++)
            {
                var special = _specials[i];
                if (special == null || _specialHealth[i] == null || _specialHealth[i].IsDead)
                {
                    continue;
                }

                Vector3 position = special.transform.position;
                var cell = Exploration.CellAt(position);
                if (!exploration.IsExplored(cell.x, cell.y))
                {
                    continue;
                }

                frame.PlaceMarker(shown++, ToCell(position), special.Numbers.Color);
            }

            frame.HideMarkers(shown);
        }

        private static Vector2 ToCell(Vector3 position)
        {
            return new Vector2(position.x / LevelMap.CellSize, -position.z / LevelMap.CellSize);
        }
    }
}
