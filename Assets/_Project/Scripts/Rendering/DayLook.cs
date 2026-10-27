using System;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>La luce di un momento del giorno nel borgo (D5 della M18): sole, ambiente, nebbia e colore dell'immagine.</summary>
    [Serializable]
    public struct DayLook
    {
        [Tooltip("Altezza del sole sull'orizzonte e da dove viene, in gradi. Di notte è la luna.")]
        [SerializeField] private Vector2 _sunAngles;

        [SerializeField] private Color _sunColor;

        [SerializeField, Min(0f)] private float _sunIntensity;

        [Tooltip("La luce ambiente, a colore unico.")]
        [SerializeField] private Color _ambient;

        [Tooltip("Nebbia e fondo della camera, dello stesso colore del cielo.")]
        [SerializeField] private Color _fog;

        [Tooltip("Dove comincia e dove finisce la nebbia, in metri dalla camera.")]
        [SerializeField] private Vector2 _fogRange;

        [Tooltip("Esposizione del post-processing, in stop.")]
        [SerializeField, Range(-2f, 2f)] private float _exposure;

        [SerializeField] private Color _colorFilter;

        [SerializeField, Range(-100f, 100f)] private float _saturation;

        [Tooltip("Temperatura del bilanciamento del bianco: positiva calda, negativa fredda.")]
        [SerializeField, Range(-100f, 100f)] private float _temperature;

        public DayLook(Vector2 sunAngles, Color sunColor, float sunIntensity, Color ambient, Color fog, Vector2 fogRange,
            float exposure, Color colorFilter, float saturation, float temperature)
        {
            _sunAngles = sunAngles;
            _sunColor = sunColor;
            _sunIntensity = sunIntensity;
            _ambient = ambient;
            _fog = fog;
            _fogRange = fogRange;
            _exposure = exposure;
            _colorFilter = colorFilter;
            _saturation = saturation;
            _temperature = temperature;
        }

        public Vector2 SunAngles => _sunAngles;

        public Color SunColor => _sunColor;

        public float SunIntensity => _sunIntensity;

        public Color Ambient => _ambient;

        public Color Fog => _fog;

        public Vector2 FogRange => _fogRange;

        public float Exposure => _exposure;

        public Color ColorFilter => _colorFilter;

        public float Saturation => _saturation;

        public float Temperature => _temperature;

        /// <summary>A metà strada tra due momenti: <paramref name="t"/> 0 è <paramref name="a"/>, 1 è <paramref name="b"/>.</summary>
        public static DayLook Lerp(DayLook a, DayLook b, float t)
        {
            return new DayLook(
                new Vector2(Mathf.Lerp(a._sunAngles.x, b._sunAngles.x, t), Mathf.LerpAngle(a._sunAngles.y, b._sunAngles.y, t)),
                Color.Lerp(a._sunColor, b._sunColor, t),
                Mathf.Lerp(a._sunIntensity, b._sunIntensity, t),
                Color.Lerp(a._ambient, b._ambient, t),
                Color.Lerp(a._fog, b._fog, t),
                Vector2.Lerp(a._fogRange, b._fogRange, t),
                Mathf.Lerp(a._exposure, b._exposure, t),
                Color.Lerp(a._colorFilter, b._colorFilter, t),
                Mathf.Lerp(a._saturation, b._saturation, t),
                Mathf.Lerp(a._temperature, b._temperature, t));
        }
    }
}
