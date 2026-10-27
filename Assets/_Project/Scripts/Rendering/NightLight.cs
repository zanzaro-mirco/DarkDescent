using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Una luce che si accende dal tramonto all'alba (D5 della M18): le lanterne e le finestre del borgo.
    /// La accende la <see cref="DayCycleView"/> del livello, con quanto è buio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class NightLight : MonoBehaviour
    {
        private Light _light;
        private float _intensity;

        public float Lit { get; private set; }

        /// <summary>0 spenta, 1 accesa del tutto: in mezzo nel passaggio tra giorno e tramonto.</summary>
        public void SetLit(float amount)
        {
            Init();
            Lit = Mathf.Clamp01(amount);
            _light.enabled = Lit > 0.01f;
            _light.intensity = _intensity * Lit;
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (_light != null)
            {
                return;
            }

            _light = GetComponent<Light>();
            _intensity = _light.intensity;
        }
    }
}
