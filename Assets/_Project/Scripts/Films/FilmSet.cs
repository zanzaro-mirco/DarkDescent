using System.Collections.Generic;
using DarkDescent.Levels;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il set di un filmato (D2 e D3 della M19): mette le comparse e gli oggetti nel livello in cui si
    /// gioca, li muove con il tempo della Timeline e alla fine rimette tutto com'era. Tutto quello che si
    /// vede dipende solo dal secondo del filmato, che la Timeline gli passa con <see cref="Show"/>: lo si
    /// può fermare, riavvolgere e fotografare in qualsiasi istante. La camera è una sola, una camera di
    /// Cinemachine senza movimenti suoi, messa a mano a ogni fotogramma: tra un'inquadratura e l'altra lo
    /// stacco è netto; alla fine la camera del gioco la riprende con la sua sfumatura.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public abstract class FilmSet : MonoBehaviour
    {
        // come la camera del gioco: abbastanza lontana da non tagliare niente
        private const float CameraDistance = 20f;
        private const int CameraPriority = 100;

        private readonly List<GameObject> _props = new List<GameObject>();
        private readonly List<Renderer> _heroRenderers = new List<Renderer>();
        private readonly List<Light> _heroLights = new List<Light>();
        private CinemachineCamera _camera;
        private CinemachineBrain _brain;
        private CinemachineBlendDefinition _blend;

        /// <summary>Le inquadrature del filmato.</summary>
        public abstract FilmScript Script { get; }

        public PlayableDirector Director => GetComponent<PlayableDirector>();

        /// <summary>Il set è montato nel livello e non ancora smontato.</summary>
        public bool IsBuilt { get; private set; }

        /// <summary>
        /// Monta il set nel livello <paramref name="level"/>, con il cavaliere <paramref name="hero"/>
        /// e la camera del gioco <paramref name="gameplay"/>, che il filmato riprende alla fine.
        /// </summary>
        public void Build(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            var go = new GameObject("FilmCamera");
            go.transform.SetParent(transform, false);
            _camera = go.AddComponent<CinemachineCamera>();
            _camera.Priority = CameraPriority;

            // dentro il filmato si entra con uno stacco, non con la sfumatura del gioco
            _brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (_brain != null)
            {
                _blend = _brain.DefaultBlend;
                _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            Stage(level, hero, gameplay);
            IsBuilt = true;
        }

        /// <summary>Il set al secondo <paramref name="time"/> del filmato.</summary>
        public void Show(float time)
        {
            if (IsBuilt)
            {
                Pose(time);
            }
        }

        /// <summary>Smonta il set: il livello torna com'era e la camera del gioco riprende, sfumando.</summary>
        public void Teardown()
        {
            if (!IsBuilt)
            {
                return;
            }

            IsBuilt = false;
            Strike();
            foreach (var prop in _props)
            {
                if (prop != null)
                {
                    Destroy(prop);
                }
            }

            _props.Clear();
            ShowHero();
            if (_brain != null)
            {
                _brain.DefaultBlend = _blend;
            }

            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }
        }

        private void OnDestroy()
        {
            Teardown();
        }

        protected abstract void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay);

        protected abstract void Pose(float time);

        protected abstract void Strike();

        /// <summary>
        /// La camera sopra <paramref name="target"/>, girata di <paramref name="yaw"/> gradi da nord e
        /// inclinata di <paramref name="pitch"/>, con la vista alta <paramref name="size"/> metri a metà.
        /// </summary>
        protected void Frame(Vector3 target, float yaw, float pitch, float size)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            _camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * CameraDistance, rotation);
            _camera.Lens.OrthographicSize = size;
        }

        /// <summary>Un oggetto del set: si distrugge quando il set si smonta.</summary>
        protected GameObject Prop(GameObject prop)
        {
            _props.Add(prop);
            return prop;
        }

        /// <summary>Nasconde il cavaliere vero, che nel filmato è una copia: torna visibile allo smontaggio.</summary>
        protected void HideHero(GameObject hero)
        {
            foreach (var part in hero.GetComponentsInChildren<Renderer>())
            {
                if (part.enabled)
                {
                    part.enabled = false;
                    _heroRenderers.Add(part);
                }
            }

            foreach (var light in hero.GetComponentsInChildren<Light>())
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    _heroLights.Add(light);
                }
            }
        }

        private void ShowHero()
        {
            foreach (var part in _heroRenderers)
            {
                if (part != null)
                {
                    part.enabled = true;
                }
            }

            foreach (var light in _heroLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }

            _heroRenderers.Clear();
            _heroLights.Clear();
        }

        /// <summary>Il centro della cella del primo segno <paramref name="symbol"/> della mappa; zero se non c'è.</summary>
        protected static Vector3 Marker(LevelMap map, char symbol)
        {
            foreach (var marker in map.Markers)
            {
                if (marker.Symbol == symbol)
                {
                    return LevelMap.CellCenter(marker.X, marker.Y);
                }
            }

            return Vector3.zero;
        }

        /// <summary>Da 0 a 1 senza strappi, all'inizio e alla fine.</summary>
        protected static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
