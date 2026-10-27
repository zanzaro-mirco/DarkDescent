using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il giorno che passa nel borgo (D5 della M18): dal tempo di gioco il momento, e da lì sole, luce
    /// ambiente, nebbia, fondo della camera e colore dell'immagine, più lanterne e finestre accese dal
    /// tramonto all'alba. Sta sul prato del borgo, con il sole: nella cripta non c'è, e il dungeon non
    /// cambia. Il colore passa da un volume suo, sopra quello del tileset, che tocca solo esposizione,
    /// filtro, saturazione e temperatura.
    /// </summary>
    [DisallowMultipleComponent]
    public class DayCycleView : MonoBehaviour
    {
        // sopra il volume del tileset, che ha priorità 0
        private const float GradePriority = 1f;

        [SerializeField] private DayLighting _lighting;
        [SerializeField] private Light _sun;

        private Func<float> _clock;
        private Camera _camera;
        private Color _cameraBackground;
        private NightLight[] _lights;
        private VolumeProfile _profile;
        private ColorAdjustments _adjustments;
        private WhiteBalance _balance;

        public DayPhase Phase { get; private set; }

        public DayLighting Lighting => _lighting;

        public Light Sun => _sun;

        /// <summary>
        /// Da dove viene l'ora (il tempo di gioco della partita) e la camera di cui colorare il fondo; il
        /// fondo torna com'era quando il borgo se ne va.
        /// </summary>
        public void Bind(Func<float> clock, Camera camera)
        {
            RestoreCamera();
            _clock = clock;
            _camera = camera;
            if (_camera != null)
            {
                _cameraBackground = _camera.backgroundColor;
            }

            if (_clock != null)
            {
                Show(_clock());
            }
        }

        /// <summary>Il borgo all'ora di questo tempo di gioco. Anche per le foto dall'editor, senza orologio.</summary>
        public void Show(float playTime)
        {
            Init();
            Phase = DayCycle.At(playTime);
            var look = _lighting.Look(Phase);

            _sun.transform.rotation = Quaternion.Euler(look.SunAngles.x, look.SunAngles.y, 0f);
            _sun.color = look.SunColor;
            _sun.intensity = look.SunIntensity;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _lighting.Ambient(look);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogStartDistance = look.FogRange.x;
            RenderSettings.fogEndDistance = look.FogRange.y;
            if (_camera != null)
            {
                _camera.backgroundColor = look.Fog;
            }

            _adjustments.postExposure.value = look.Exposure;
            _adjustments.colorFilter.value = look.ColorFilter;
            _adjustments.saturation.value = look.Saturation;
            _balance.temperature.value = look.Temperature;

            float lit = DayCycle.Lights(playTime);
            foreach (var light in _lights)
            {
                light.SetLit(lit);
            }
        }

        private void Init()
        {
            if (_profile != null)
            {
                return;
            }

            // le lanterne e le finestre di tutto il livello, già costruito quando la vista si accende
            var lights = new System.Collections.Generic.List<NightLight>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                lights.AddRange(root.GetComponentsInChildren<NightLight>(true));
            }

            _lights = lights.ToArray();

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.hideFlags = HideFlags.DontSave;
            _adjustments = _profile.Add<ColorAdjustments>();
            _adjustments.postExposure.overrideState = true;
            _adjustments.colorFilter.overrideState = true;
            _adjustments.saturation.overrideState = true;
            _balance = _profile.Add<WhiteBalance>();
            _balance.temperature.overrideState = true;

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = GradePriority;
            volume.sharedProfile = _profile;
        }

        private void Update()
        {
            if (_clock != null)
            {
                Show(_clock());
            }
        }

        private void RestoreCamera()
        {
            if (_camera != null)
            {
                _camera.backgroundColor = _cameraBackground;
            }

            _camera = null;
        }

        private void OnDestroy()
        {
            RestoreCamera();
            if (_profile == null)
            {
                return;
            }

            foreach (var component in _profile.components)
            {
                Release(component);
            }

            Release(_profile);
        }

        private static void Release(Object asset)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(asset);
            }
            else
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
