using System;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Tiene l'esplorazione del livello corrente (D15 della scheda M6): a ogni cambio di cella del
    /// cavaliere scopre i dintorni e avvisa. Sta in Core; il CompositionRoot gli passa il cavaliere
    /// e la mappa di ogni livello caricato. Senza mappa (i livelli fatti a mano) non fa niente.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplorationTracker : MonoBehaviour
    {
        [Tooltip("Quanti passi sul pavimento si scoprono attorno al cavaliere: 3 celle sono 12 metri, circa quanto inquadra la camera.")]
        [SerializeField, Min(0)] private int _revealSteps = 3;

        private Transform _player;
        private Vector2Int _lastCell;

        /// <summary>È cambiato il livello: nuova mappa, o nessuna.</summary>
        public event Action LevelChanged;

        /// <summary>Si è scoperta almeno una cella nuova.</summary>
        public event Action Explored;

        /// <summary>L'esplorazione del livello corrente; null se il livello non ha una mappa.</summary>
        public Exploration Exploration { get; private set; }

        public Transform Player => _player;

        public void Bind(Transform player)
        {
            _player = player;
        }

        /// <summary>
        /// Un livello nuovo: si riparte da niente, e si scopre subito attorno all'ingresso. Con
        /// <paramref name="keepExplored"/> lo stesso livello ricostruito dopo Ricomincia: la mappa
        /// scoperta resta.
        /// </summary>
        public void SetLevel(LevelMap map, bool keepExplored = false)
        {
            var previous = keepExplored ? Exploration : null;
            Exploration = map != null ? new Exploration(map) : null;
            Exploration?.CopyExplored(previous);
            LevelChanged?.Invoke();
            if (Exploration != null && _player != null)
            {
                _lastCell = Exploration.CellAt(_player.position);
                Exploration.Reveal(_lastCell, _revealSteps);
                Explored?.Invoke();
            }
        }

        // un confronto tra due celle per frame; il lavoro vero solo quando il cavaliere passa in un'altra
        private void Update()
        {
            if (Exploration == null || _player == null)
            {
                return;
            }

            var cell = Exploration.CellAt(_player.position);
            if (cell == _lastCell)
            {
                return;
            }

            _lastCell = cell;
            if (Exploration.Reveal(cell, _revealSteps))
            {
                Explored?.Invoke();
            }
        }
    }
}
