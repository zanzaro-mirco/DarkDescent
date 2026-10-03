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

        [Tooltip("Gli altri marcatori: nemici e oggetti di scena, al centro della cella.")]
        [SerializeField] private MarkerPrefab[] _markers;

        public GameObject Floor => _floor;

        public GameObject Wall => _wall;

        public GameObject LowWall => _lowWall;

        public GameObject WallTorch => _wallTorch;

        public GameObject StairsDown => _stairsDown;

        public GameObject ExitBanner => _exitBanner;

        public Material HighlightMaterial => _highlightMaterial;

        public Color AmbientColor => _ambientColor;

        public VolumeProfile PostProcessing => _postProcessing;

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
