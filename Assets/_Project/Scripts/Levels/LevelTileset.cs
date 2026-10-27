using DarkDescent.Audio;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I moduli con cui si costruisce un livello dalla sua mappa, e la sua atmosfera: luce ambiente
    /// e post-processing. Dati immutabili: chi costruisce legge, non scrive. Oggi lo usa lo
    /// strumento di editor, alla M6 il generatore a runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "Tileset", menuName = "DarkDescent/Level Tileset")]
    public class LevelTileset : ScriptableObject
    {
        [SerializeField] private GameObject _floor;

        [Tooltip("Muro alto, sui lati nord ed est delle stanze: quelli lontani dalla camera.")]
        [SerializeField] private GameObject _wall;

        [Tooltip("Muro basso, sui lati sud e ovest: quelli tra la camera e il cavaliere (D5).")]
        [SerializeField] private GameObject _lowWall;

        [Tooltip("Il terreno sotto tutta la mappa e oltre il bordo, fuori dal NavMesh (il borgo, M18). Vuoto: il nero di sotto terra.")]
        [SerializeField] private GameObject _backdrop;

        [Tooltip("Erba e sassi sparsi sul terreno (il borgo, M18): uno per cella, scelto dall'hash della cella. Vuoto: niente.")]
        [SerializeField] private GameObject[] _scatter;

        [Tooltip("Su quante celle di pavimento libero va qualcosa di sparso.")]
        [SerializeField, Range(0f, 1f)] private float _scatterChance = 0.5f;

        [Tooltip("Su quante celle fuori dal livello (roccia e terreno aperto) va qualcosa di sparso.")]
        [SerializeField, Range(0f, 1f)] private float _outsideScatterChance = 0.7f;

        [Tooltip("Variante del pavimento, su alcune celle (le caverne: terra con i sassi). Vuoto: nessuna.")]
        [SerializeField] private GameObject _floorVariant;

        [Tooltip("Variante del muro alto, su alcuni lati (le caverne: muro rotto). Vuoto: nessuna.")]
        [SerializeField] private GameObject _wallVariant;

        [Tooltip("Su quanti pezzi va la variante. Scelta dalla posizione: stesso livello, stessi pezzi.")]
        [SerializeField, Range(0f, 1f)] private float _variantChance = 0.25f;

        [Tooltip("Torcia da muro, appesa al muro alto più vicino della sua cella.")]
        [SerializeField] private GameObject _wallTorch;

        [Tooltip("Scala che scende: prende il posto del pavimento della sua cella.")]
        [SerializeField] private GameObject _stairsDown;

        [Tooltip("Stendardo appeso al muro alto oltre la scala: segnala l'uscita da lontano.")]
        [SerializeField] private GameObject _exitBanner;

        [Tooltip("Il materiale dei moduli con l'emissione accesa: l'uscita sotto il cursore.")]
        [SerializeField] private Material _highlightMaterial;

        [Tooltip("Luce ambiente del livello, a colore unico: quasi nero, il resto lo fanno torce e cavaliere.")]
        [SerializeField] private Color _ambientColor = new Color(0.03f, 0.03f, 0.045f);

        [Tooltip("Post-processing del livello: tonemapping, bloom, vignetta. Volume globale nella scena del livello.")]
        [SerializeField] private VolumeProfile _postProcessing;

        [Tooltip("Musica, fondo e versi nel buio del livello (passo 7.0 della M7).")]
        [SerializeField] private AmbienceProfile _ambience;

        [Tooltip("I passi del cavaliere su questo pavimento: pietra nella cripta, terra nelle caverne.")]
        [SerializeField] private AudioClip[] _footsteps;

        [Tooltip("Il nome del tipo di livello nella tabella delle stringhe, per la scritta \"Cripta – Livello 3\".")]
        [SerializeField] private string _nameKey;

        [Tooltip("Il prefab degli oggetti a terra: i marcatori 'i', con l'oggetto preso dalla direttiva @items.")]
        [SerializeField] private GameObject _groundItem;

        [Tooltip("Gli altri marcatori: nemici e oggetti di scena, al centro della cella.")]
        [SerializeField] private MarkerPrefab[] _markers;

        [Tooltip("Il materiale delle macchie a terra, con l'atlante (D5 della M17); senza, il livello non ne ha.")]
        [SerializeField] private Material _decalMaterial;

        [Tooltip("Quanto stanno sopra la cima delle piastrelle le macchie, in metri: abbastanza da non sfarfallare.")]
        [SerializeField, Range(0.005f, 0.1f)] private float _decalLift = 0.02f;

        public GameObject Floor => _floor;

        public GameObject Backdrop => _backdrop;

        public GameObject[] Scatter => _scatter;

        public float ScatterChance => _scatterChance;

        public float OutsideScatterChance => _outsideScatterChance;

        public Material DecalMaterial => _decalMaterial;

        public float DecalLift => _decalLift;

        public GameObject FloorVariant => _floorVariant;

        public GameObject WallVariant => _wallVariant;

        public float VariantChance => _variantChance;

        public GameObject GroundItem => _groundItem;

        public GameObject Wall => _wall;

        public GameObject LowWall => _lowWall;

        public GameObject WallTorch => _wallTorch;

        public GameObject StairsDown => _stairsDown;

        public GameObject ExitBanner => _exitBanner;

        public Material HighlightMaterial => _highlightMaterial;

        public Color AmbientColor => _ambientColor;

        public VolumeProfile PostProcessing => _postProcessing;

        public AmbienceProfile Ambience => _ambience;

        public AudioClip[] Footsteps => _footsteps;

        public string NameKey => _nameKey;

        /// <summary>Il prefab di questo simbolo porta il suo pavimento: il costruttore non mette quello di sempre.</summary>
        public bool ReplacesFloor(char symbol)
        {
            foreach (var marker in _markers)
            {
                if (marker.Symbol == symbol)
                {
                    return marker.ReplacesFloor;
                }
            }

            return false;
        }

        /// <summary>Il prefab di questo simbolo ha già il suo verso: il costruttore non lo gira.</summary>
        public bool KeepsRotation(char symbol)
        {
            foreach (var marker in _markers)
            {
                if (marker.Symbol == symbol)
                {
                    return marker.KeepsRotation;
                }
            }

            return false;
        }

        public GameObject GetMarkerPrefab(char symbol)
        {
            foreach (var marker in _markers)
            {
                if (marker.Symbol == symbol)
                {
                    return marker.Prefab;
                }
            }

            return null;
        }
    }
}
