using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il cerchio rosso a terra dell'anello di fuoco del boss (D8 della M10): lo stesso disegno del
    /// settore del bruto, a giro intero. Appare con il raggio vero dell'anello, si riempie dal centro
    /// mentre carica e sparisce quando l'anello parte. Chi decide quando è il boss.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegraphCircle : MonoBehaviour
    {
        [Tooltip("Non illuminato e trasparente: il fondo del cerchio.")]
        [SerializeField] private Material _material;

        [Tooltip("Il riempimento che cresce durante la carica.")]
        [SerializeField] private Material _fillMaterial;

        [Tooltip("Altezza sopra il pavimento: abbastanza per non sparire nei sassi della terra.")]
        [SerializeField, Min(0f)] private float _height = 0.09f;

        [SerializeField, Range(8, 96)] private int _segments = 48;

        private GameObject _circle;
        private Transform _fill;
        private float _elapsed;
        private float _duration;

        /// <summary>Il cerchio si vede: per i test.</summary>
        public bool IsShown => _circle != null && _circle.activeSelf;

        private void Awake()
        {
            var mesh = TelegraphSector.BuildMesh(360f, _segments);
            _circle = TelegraphSector.CreatePart("Telegraph", transform, mesh, _material);
            _circle.transform.localPosition = Vector3.up * _height;
            _fill = TelegraphSector.CreatePart("Fill", _circle.transform, mesh, _fillMaterial).transform;
            _fill.localPosition = Vector3.up * 0.01f;
            _circle.SetActive(false);
        }

        /// <summary>Mostra il cerchio di raggio <paramref name="radius"/>, pieno dopo <paramref name="duration"/> secondi.</summary>
        public void Show(float radius, float duration)
        {
            // la scala del boss non deve cambiare il raggio: il cerchio è in metri veri
            Vector3 parentScale = transform.lossyScale;
            _circle.transform.localScale = new Vector3(radius / parentScale.x, 1f, radius / parentScale.z);
            _elapsed = 0f;
            _duration = duration;
            _fill.localScale = new Vector3(0f, 1f, 0f);
            _circle.SetActive(true);
        }

        public void Hide()
        {
            if (_circle != null)
            {
                _circle.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_circle.activeSelf)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            _fill.localScale = new Vector3(t, 1f, t);
        }
    }
}
