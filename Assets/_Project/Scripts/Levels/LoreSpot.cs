using System;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Un testo del mondo da leggere (D9 della M25): un leggio, una lapide con un'incisione o un mucchio di carte, messi contro un muro del livello. Un click
    /// e il cavaliere ci va e lo legge: chi tiene la partita apre il pannello. Un testo non letto ha un bagliore tenue di candela; letto, il bagliore
    /// si spegne, e il ricordo del livello (ADR-079) lo sa. Si può rileggere quando si vuole.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class LoreSpot : MonoBehaviour, ILevelSecret
    {
        /// <summary>
        /// Acceso nel gioco, spento dai test PlayMode (D14 della M25): l'arredo è un oggetto in più contro un muro. Lo accendono solo i test della lore.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        [Tooltip("Il bagliore tenue di un testo non letto.")]
        [SerializeField] private Light _glow;

        private Interactable _interactable;

        /// <summary>Il numero del testo (1–12), nella tabella delle stringhe come <c>lore.N.title</c> e <c>lore.N.text</c>.</summary>
        public int TextId { get; private set; }

        public bool IsDone { get; private set; }

        /// <summary>La cella del livello dove sta: gli altri segreti stanno lontano.</summary>
        public Vector2Int Cell { get; private set; }

        public LoreKind Kind => LoreRules.KindOf(TextId);

        /// <summary>Il cavaliere ha letto: chi tiene la partita apre il pannello con questo testo.</summary>
        public event Action<LoreSpot> Reading;

        /// <summary>Lo mette contro il muro del suo posto, voltato verso lo spazio libero, e gli dà il suo testo.</summary>
        public static LoreSpot Build(LevelContext level, GameObject prefab, SecretSpots.Spot spot, int textId)
        {
            Vector3 facing = new Vector3(spot.Facing.x, 0f, -spot.Facing.y);
            var instance = Instantiate(prefab, LevelMap.CellCenter(spot.Cell.x, spot.Cell.y) - facing * 1.3f, Quaternion.LookRotation(facing), level.transform);
            instance.name = "Lore" + textId;
            var lore = instance.GetComponent<LoreSpot>();
            lore.TextId = textId;
            lore.Cell = spot.Cell;
            return lore;
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        /// <summary>Già letto in una visita di prima (ADR-079): il bagliore spento.</summary>
        public void RestoreDone()
        {
            IsDone = true;
            if (_glow != null)
            {
                _glow.enabled = false;
            }
        }

        private void HandleUsed(GameObject user)
        {
            RestoreDone();
            Reading?.Invoke(this);
        }
    }
}
