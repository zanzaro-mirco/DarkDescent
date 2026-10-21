using System;
using System.Collections;
using System.Collections.Generic;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Una cassa che si apre con un click (D7 della scheda M6): il cavaliere ci va, il coperchio si
    /// alza, e davanti cade un oggetto della sua loot table, a volte con una pozione (D14). Una volta
    /// sola. Il seme viene dalla cella, come per i nemici (ADR-029): la stessa cassa dà lo stesso
    /// oggetto. La collega il CompositionRoot. Forziere e tesoro del re (D8 della M12) sono casse con
    /// un bonus: oggetti in più ai lati, una rarità garantita e più oro; il tesoro resta sigillato finché
    /// il re è vivo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Chest : MonoBehaviour
    {
        // la cella in decimetri, come LootDrop: la cassa non si muove, ma il seme resta della cella
        private const float CellsPerMeter = 10f;

        [SerializeField] private LootTable _lootTable;

        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Il coperchio: ruota sul suo perno, che nel modello di KayKit è la cerniera dietro.")]
        [SerializeField] private Transform _lid;

        [Tooltip("Di quanto si alza il coperchio, in gradi.")]
        [SerializeField] private float _openAngle = 110f;

        [SerializeField, Min(0.05f)] private float _openDuration = 0.35f;

        [Tooltip("Il collider del click: si spegne quando la cassa è aperta, e il cursore non la vede più.")]
        [SerializeField] private Collider _clickArea;

        [Tooltip("Quanto davanti alla cassa cade l'oggetto.")]
        [SerializeField, Min(0f)] private float _dropDistance = 1.3f;

        [Tooltip("Quanto di lato all'oggetto cade la pozione, se c'è.")]
        [SerializeField, Min(0f)] private float _potionSideOffset = 0.7f;

        [Tooltip("Da che altezza partono gli oggetti in volo.")]
        [SerializeField, Min(0f)] private float _dropHeight = 0.7f;

        [Tooltip("Il loot in più del forziere e del tesoro del re (D8 della M12); la cassa di sempre non ne ha.")]
        [SerializeField] private LootBonus _bonus = new LootBonus();

        [Tooltip("Sigillata finché il re è vivo: niente click fino a Unseal (il tesoro del re, D8 della M12).")]
        [SerializeField] private bool _sealedUntilBossDies;

        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip[] _openClips;

        private Interactable _interactable;
        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;

        // riusata a ogni apertura: gli oggetti in più non creano liste nuove
        private readonly List<ItemInstance> _extra = new List<ItemInstance>();

        /// <summary>La cassa si è aperta; l'argomento è l'oggetto caduto, null se non ne è caduto nessuno.</summary>
        public event Action<GroundItem> Opened;

        public bool IsOpen { get; private set; }

        /// <summary>Il tesoro del re con il re ancora vivo: non si clicca.</summary>
        public bool IsSealed { get; private set; }

        /// <summary>Si apre solo dopo la morte del re: il tesoro del re (D8 della M12).</summary>
        public bool OpensWithBoss => _sealedUntilBossDies;

        public LootBonus Bonus => _bonus;

        public LootTable LootTable => _lootTable;

        /// <summary>L'oggetto in più che porta, come la pergamena del livello (D5 della M9), o null.</summary>
        public ItemInstance Carried { get; private set; }

        /// <summary>Gli dà un oggetto in più da lasciare insieme al suo loot: la pergamena del livello.</summary>
        public void Carry(ItemInstance item)
        {
            Carried = item;
        }

        public void Bind(LootRoller roller, int depth)
        {
            _roller = roller;
            _depth = depth;
        }

        /// <summary>Quello che lascerà aprendosi, senza cambiare niente: per i test e per rigiocare un seme.</summary>
        public ItemInstance Preview()
        {
            // il forziere lascia solo il suo tiro in più (D13 della M13)
            if (_bonus != null && _bonus.ReplacesRoll)
            {
                return null;
            }

            return _roller?.Roll(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>
        /// La pozione che lascerà in più aprendosi, senza cambiare niente; null se non ne lascia. Una
        /// cassa senza oggetto può darla al posto dell'oggetto (D13 della M13).
        /// </summary>
        public ItemInstance PreviewPotion()
        {
            var potion = _roller?.RollPotion(_lootTable, _depth, _cellX, _cellZ);
            if (potion == null && Comfort() == ChestComfort.Potion && _lootTable.Potion != null)
            {
                potion = new ItemInstance(_lootTable.Potion);
            }

            return potion;
        }

        /// <summary>
        /// L'oro che lascerà in più, senza cambiare niente; 0 se non ne lascia (D4 della M10). Una cassa
        /// senza oggetto e senza la pozione di compenso ne dà il doppio (D13 della M13).
        /// </summary>
        public int PreviewGold()
        {
            int gold = _roller?.RollGold(_lootTable, _depth, _cellX, _cellZ, _bonus) ?? 0;
            return Comfort() == ChestComfort.Gold ? gold * 2 : gold;
        }

        private enum ChestComfort
        {
            None,
            Potion,
            Gold,
        }

        // Senza oggetto, al suo posto una pozione o il doppio dell'oro: un tiro suo della cella; se la
        // pozione c'è già per conto suo, l'oro
        private ChestComfort Comfort()
        {
            if (_roller == null || (_bonus != null && _bonus.ExtraRolls > 0) || Preview() != null)
            {
                return ChestComfort.None;
            }

            bool potion = _roller.RollChestComfort(_depth, _cellX, _cellZ);
            if (potion && _roller.RollPotion(_lootTable, _depth, _cellX, _cellZ) == null)
            {
                return ChestComfort.Potion;
            }

            return ChestComfort.Gold;
        }

        /// <summary>Gli oggetti in più che lascerà, in <paramref name="into"/>, senza cambiare niente; nessuno per la cassa di sempre.</summary>
        public void PreviewExtra(List<ItemInstance> into)
        {
            into.Clear();
            _roller?.RollExtra(_lootTable, _bonus, Preview(), _depth, _cellX, _cellZ, into);
        }

        /// <summary>Il re è morto: il tesoro si può aprire. Su una cassa già aperta non riaccende il click.</summary>
        public void Unseal()
        {
            if (!IsSealed)
            {
                return;
            }

            IsSealed = false;
            if (_clickArea != null && !IsOpen)
            {
                _clickArea.enabled = true;
            }
        }

        /// <summary>
        /// Già aperta in una visita di prima (10.9 della M10): coperchio alzato, niente click, e non
        /// lascia niente. Quello che aveva lasciato è tra gli oggetti a terra del ricordo del livello.
        /// </summary>
        public void RestoreOpen()
        {
            IsOpen = true;
            Carried = null;
            if (_clickArea != null)
            {
                _clickArea.enabled = false;
            }

            if (_lid != null)
            {
                _lid.localRotation *= Quaternion.Euler(-_openAngle, 0f, 0f);
            }
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _cellX = Mathf.RoundToInt(transform.position.x * CellsPerMeter);
            _cellZ = Mathf.RoundToInt(transform.position.z * CellsPerMeter);
            IsSealed = _sealedUntilBossDies;
            if (IsSealed && _clickArea != null)
            {
                _clickArea.enabled = false;
            }
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void HandleUsed(GameObject user)
        {
            if (IsOpen || IsSealed)
            {
                return;
            }

            IsOpen = true;

            // spento il click, il cursore passa oltre: niente più nome né evidenziazione
            _interactable.SetHighlighted(false);
            if (_clickArea != null)
            {
                _clickArea.enabled = false;
            }

            if (_audio != null && _openClips.Length > 0)
            {
                // la variante dalla cella, non da Random: la stessa cassa suona sempre uguale
                _audio.PlayOneShot(_openClips[Mathf.Abs(_cellX + _cellZ) % _openClips.Length]);
            }

            if (_lid != null)
            {
                StartCoroutine(OpenLid());
            }

            GroundItem dropped = null;
            Vector3 front = transform.position + transform.forward * _dropDistance;

            // gli oggetti saltano fuori dalla cassa aperta
            Vector3 from = transform.position + Vector3.up * _dropHeight;
            var item = Preview();
            if (item != null && _groundItemPrefab != null)
            {
                dropped = GroundItem.Spawn(_groundItemPrefab, item, front, from);
            }

            var potion = PreviewPotion();
            if (potion != null && _groundItemPrefab != null)
            {
                GroundItem.Spawn(_groundItemPrefab, potion, front + transform.right * _potionSideOffset, from);
            }

            // la pergamena dall'altra parte: i tre oggetti non si coprono
            if (Carried != null && _groundItemPrefab != null)
            {
                GroundItem.Spawn(_groundItemPrefab, Carried, front - transform.right * _potionSideOffset, from);
                Carried = null;
            }

            // l'oro un passo più avanti dell'oggetto
            int gold = PreviewGold();
            if (gold > 0 && _groundItemPrefab != null)
            {
                GroundItem.SpawnGold(_groundItemPrefab, _lootTable.Gold, gold, front + transform.forward * _potionSideOffset, from);
            }

            // gli oggetti in più sulla stessa riga, oltre pozione e pergamena, alternati destra e sinistra
            PreviewExtra(_extra);
            for (int i = 0; i < _extra.Count && _groundItemPrefab != null; i++)
            {
                float side = (i % 2 == 0 ? 1f : -1f) * (2 + i / 2) * _potionSideOffset;
                GroundItem.Spawn(_groundItemPrefab, _extra[i], front + transform.right * side, from);
            }

            _extra.Clear();
            Opened?.Invoke(dropped);
        }

        // Il davanti della cassa è il suo +Z: ruotando attorno a X in negativo il bordo davanti si alza.
        private IEnumerator OpenLid()
        {
            Quaternion closed = _lid.localRotation;
            Quaternion open = closed * Quaternion.Euler(-_openAngle, 0f, 0f);
            for (float t = 0f; t < _openDuration; t += Time.deltaTime)
            {
                _lid.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t / _openDuration));
                yield return null;
            }

            _lid.localRotation = open;
        }
    }
}
