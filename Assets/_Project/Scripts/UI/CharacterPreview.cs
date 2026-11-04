using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Classes;
using DarkDescent.Films;
using DarkDescent.Items;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il modello che gira nella creazione del personaggio (D8 della M21): una copia del cavaliere per ogni
    /// classe, con il suo corpo e la sua arma in mano, su un piccolo palco lontano dal gioco, ripreso da una
    /// camera che scrive su una texture. Le copie sono quelle dei filmati (<see cref="FilmActor"/>): senza
    /// niente del gioco addosso, in posa di riposo. La camera si accende solo mentre la schermata è aperta.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterPreview : MonoBehaviour
    {
        // sotto il gioco, dove nessuna camera guarda: la camera del palco vede solo le sue copie
        private static readonly Vector3 StageCenter = new Vector3(0f, -600f, 0f);

        private const string RightHand = "handslot.r";
        private const string LeftHand = "handslot.l";

        [Tooltip("Dove si vede il palco: la texture della camera ci si scrive.")]
        [SerializeField] private RawImage _image;

        [Tooltip("La posa a riposo, in loop.")]
        [SerializeField] private AnimationClip _idle;

        [SerializeField, Min(0f)] private float _turnDegreesPerSecond = 26f;

        [SerializeField] private Vector2Int _size = new Vector2Int(560, 720);

        private readonly Dictionary<string, FilmActor> _actors = new Dictionary<string, FilmActor>();
        private Camera _camera;
        private RenderTexture _texture;
        private FilmActor _shown;
        private GameObject _stage;

        public bool IsBuilt => _camera != null;

        public string ShownBody { get; private set; }

        public RawImage Image => _image;

        /// <summary>
        /// Prepara il palco, una volta: una copia di <paramref name="hero"/> per ogni classe, con il corpo
        /// della classe. Il corpo del giocatore torna a com'era.
        /// </summary>
        public void Build(GameObject hero, IReadOnlyList<CharacterClass> classes)
        {
            if (IsBuilt || hero == null || _idle == null)
            {
                return;
            }

            // alla radice della scena e non sotto la schermata: la scala dell'interfaccia lo rimpicciolirebbe
            var stage = new GameObject("CreationStage").transform;
            stage.position = StageCenter;
            _stage = stage.gameObject;
            BuildCamera(stage);
            BuildLights(stage);

            var body = hero.GetComponentInChildren<PlayerBody>(true);
            string before = body != null ? body.Current : null;
            foreach (var characterClass in classes)
            {
                if (_actors.ContainsKey(characterClass.Body) || body == null || !body.Show(characterClass.Body))
                {
                    continue;
                }

                var actor = FilmActor.Clone(hero, stage, characterClass.Body, _idle);
                actor.transform.localPosition = Vector3.zero;
                actor.Show(false);
                Arm(actor, characterClass);
                _actors[characterClass.Body] = actor;
            }

            if (body != null && !string.IsNullOrEmpty(before))
            {
                body.Show(before);
            }
        }

        /// <summary>Mostra il corpo di una classe, da davanti; un corpo che non c'è lascia il palco vuoto.</summary>
        public void Show(CharacterClass characterClass)
        {
            if (_shown != null)
            {
                _shown.Show(false);
                _shown = null;
            }

            ShownBody = null;
            if (characterClass != null && _actors.TryGetValue(characterClass.Body, out var actor))
            {
                _shown = actor;
                ShownBody = characterClass.Body;
                actor.transform.localRotation = Quaternion.identity;
                actor.Show(true);
            }
        }

        /// <summary>Smonta il palco, quando la partita comincia: le copie e la camera non servono più. <see cref="Build"/> lo rifà.</summary>
        public void Release()
        {
            _shown = null;
            ShownBody = null;
            _actors.Clear();
            if (_stage != null)
            {
                Destroy(_stage);
                _stage = null;
            }

            _camera = null;
            if (_texture != null)
            {
                if (_image != null)
                {
                    _image.texture = null;
                }

                _texture.Release();
                Destroy(_texture);
                _texture = null;
            }
        }

        /// <summary>La camera gira solo con la schermata aperta.</summary>
        public void SetRunning(bool running)
        {
            if (_camera != null)
            {
                _camera.enabled = running;
            }
        }

        private void Update()
        {
            if (_shown == null || _camera == null || !_camera.enabled)
            {
                return;
            }

            _shown.transform.Rotate(0f, _turnDegreesPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
            _shown.Pose(_idle, Time.unscaledTime);
        }

        private void OnDestroy()
        {
            Release();
        }

        private void BuildCamera(Transform stage)
        {
            _texture = new RenderTexture(_size.x, _size.y, 24, RenderTextureFormat.ARGB32) { name = "CreationPreview", antiAliasing = 4 };
            var go = new GameObject("PreviewCamera");
            go.transform.SetParent(stage, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, -5.7f);
            go.transform.localRotation = Quaternion.identity;
            _camera = go.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.06f, 0.07f, 1f);
            _camera.fieldOfView = 28f;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 24f;
            _camera.targetTexture = _texture;
            _camera.enabled = false;
            _image.texture = _texture;
        }

        // una luce calda da un lato e una fredda dall'altro: la scena intorno non ne ha, il palco sì
        private static void BuildLights(Transform stage)
        {
            Light(stage, "Key", new Vector3(1.8f, 2.8f, -2.4f), new Color(1f, 0.82f, 0.62f), 7f);
            Light(stage, "Fill", new Vector3(-2.2f, 1.4f, -2.2f), new Color(0.55f, 0.65f, 1f), 2.5f);
            Light(stage, "Back", new Vector3(0f, 2.4f, 2.2f), new Color(0.9f, 0.9f, 1f), 3f);
        }

        private static void Light(Transform stage, string name, Vector3 position, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(stage, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 9f;
            light.shadows = LightShadows.None;
        }

        // le armi in mano sono quelle della classe: il Mago ha la bacchetta e il tomo, non la spada del prefab
        private static void Arm(FilmActor actor, CharacterClass characterClass)
        {
            if (characterClass.StartingWeapon == null)
            {
                return;
            }

            Hold(actor.Model.transform, RightHand, characterClass.StartingWeapon);
            Hold(actor.Model.transform, LeftHand, characterClass.StartingOffhand);
        }

        private static void Hold(Transform model, string slot, ItemDefinition item)
        {
            var hand = Find(model, slot);
            if (hand == null)
            {
                return;
            }

            for (int i = hand.childCount - 1; i >= 0; i--)
            {
                Destroy(hand.GetChild(i).gameObject);
            }

            if (item == null || item.Model == null)
            {
                return;
            }

            var instance = Instantiate(item.Model, hand, false);
            instance.transform.SetLocalPositionAndRotation(item.HeldPosition, Quaternion.Euler(item.HeldRotation));
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var found = Find(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
