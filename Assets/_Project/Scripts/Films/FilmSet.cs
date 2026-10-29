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

        /// <summary>Da 0 a 1 senza strappi, all'inizio e alla fine.</summary>
        protected static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
