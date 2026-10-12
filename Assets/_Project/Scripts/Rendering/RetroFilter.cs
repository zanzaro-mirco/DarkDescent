using System;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il filtro retrò (D2 della M11): un passo a tutto schermo del renderer, dopo il post-processing e
    /// prima dell'interfaccia, porta ogni pixel al colore più vicino della palette con un dithering
    /// ordinato. Questo componente lo regola con proprietà globali degli shader, così nessun asset
    /// cambia giocando: la palette all'avvio, forza e dithering a ogni scelta delle opzioni. Spento, lo
    /// shader lascia passare l'immagine com'è. La scelta resta tra le preferenze.
    /// </summary>
    [DisallowMultipleComponent]
    public class RetroFilter : MonoBehaviour
    {
        public const string Preference = "retro.mode";

        private static readonly int StrengthId = Shader.PropertyToID("_RetroStrength");
        private static readonly int DitherId = Shader.PropertyToID("_RetroDither");
        private static readonly int PaletteId = Shader.PropertyToID("_RetroPalette");

        [SerializeField] private RetroPalette _palette;

        [Tooltip("Leggero: quanto dell'immagine portata alla palette si mescola a quella vera.")]
        [SerializeField, Range(0f, 1f)] private float _lightStrength = 0.6f;

        [Tooltip("Leggero: quanto si vede il dithering.")]
        [SerializeField, Range(0f, 1f)] private float _lightDither = 0.5f;

        [Tooltip("Pieno: quanto si vede il dithering.")]
        [SerializeField, Range(0f, 1f)] private float _fullDither = 1f;

        private readonly Vector4[] _colors = new Vector4[RetroPalette.Size];

        /// <summary>La scelta è cambiata: per l'etichetta delle opzioni.</summary>
        public event Action<RetroMode> ModeChanged;

        public RetroMode Mode { get; private set; } = RetroModes.Default;

        /// <summary>La scelta dopo, salvata tra le preferenze: il pulsante delle opzioni.</summary>
        public void Cycle()
        {
            SetMode(RetroModes.Next(Mode));
            PlayerPrefs.SetInt(Preference, (int)Mode);
            PlayerPrefs.Save();
        }

        public void SetMode(RetroMode mode)
        {
            Mode = mode;

            // senza palette lo shader porterebbe tutto al nero: meglio spento
            float strength = _palette == null || _palette.Count < RetroPalette.Size ? 0f : mode switch
            {
                RetroMode.Off => 0f,
                RetroMode.Light => _lightStrength,
                _ => 1f,
            };

            Shader.SetGlobalFloat(StrengthId, strength);
            Shader.SetGlobalFloat(DitherId, mode == RetroMode.Full ? _fullDither : _lightDither);
            ModeChanged?.Invoke(mode);
        }

        private void Awake()
        {
            if (_palette == null || _palette.Count < RetroPalette.Size)
            {
                Debug.LogWarning($"[DarkDescent] il filtro retrò vuole una palette di {RetroPalette.Size} colori: resta spento.", this);
            }

            for (int i = 0; i < RetroPalette.Size; i++)
            {
                // nello spazio sRGB, come lo shader confronta i colori
                Color color = _palette != null && i < _palette.Count ? _palette[i] : Color.black;
                _colors[i] = new Vector4(color.r, color.g, color.b, 1f);
            }

            Shader.SetGlobalVectorArray(PaletteId, _colors);
            SetMode(RetroModes.FromPreference(PlayerPrefs.GetInt(Preference, (int)RetroModes.Default)));
        }

        // fuori dal gioco il filtro non si vede: nell'editor, dopo il Play, l'immagine torna com'è
        private void OnDestroy()
        {
            Shader.SetGlobalFloat(StrengthId, 0f);
        }
    }
}
