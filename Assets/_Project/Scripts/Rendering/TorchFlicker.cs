using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il tremolio di una fiamma: l'intensità della luce segue un rumore di Perlin attorno al valore
    /// di partenza. Ogni torcia parte da un punto diverso del rumore, così non tremano all'unisono.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [Tooltip("Quanto si scosta l'intensità, in frazione del valore di partenza.")]
        [SerializeField, Range(0f, 0.5f)] private float _amount = 0.2f;

        [Tooltip("Velocità del tremolio: scorrimento del rumore al secondo.")]
        [SerializeField, Min(0f)] private float _speed = 3f;

        private Light _light;
        private float _baseIntensity;
        private float _seed;

        public float BaseIntensity => _baseIntensity;

        public float Amount => _amount;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;

            // dalla posizione e non da Random: due torce non partono mai dallo stesso punto, e il
            // risultato non cambia da una partita all'altra
            Vector3 p = transform.position;
            _seed = p.x * 12.9898f + p.z * 78.233f;
        }

        private void OnDisable()
        {
            _light.intensity = _baseIntensity;
        }

        private void Update()
        {
            // PerlinNoise restituisce valori tra 0 e 1 (a volte poco fuori): riportato tra -1 e 1
            float noise = Mathf.Clamp01(Mathf.PerlinNoise(_seed, Time.time * _speed)) * 2f - 1f;
            _light.intensity = _baseIntensity * (1f + noise * _amount);
        }
    }
}
