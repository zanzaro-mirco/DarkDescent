using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DarkDescent.Films
{
    /// <summary>
    /// Una comparsa dei filmati (D6 della M19): la copia di un modello del gioco, il cavaliere con quello
    /// che porta, un abitante, uno scheletro, senza niente del gioco addosso, solo le maglie e lo
    /// scheletro delle ossa. Le animazioni passano da un grafo di Playables suo, valutato a mano: la posa
    /// dipende solo dal tempo che il set le dà, e il filmato si può fermare e riprendere in qualsiasi
    /// istante. Sopra le gambe può tenere una seconda clip, dal petto in su, con un oggetto in mano: la
    /// lanterna dei paesani che scappano.
    /// </summary>
    [DisallowMultipleComponent]
    public class FilmActor : MonoBehaviour
    {
        // dal petto in su, braccia e testa: le gambe restano alla clip di sotto
        private const string UpperBody = "/chest";

        private readonly List<AnimationClip> _clips = new List<AnimationClip>();
        private readonly List<AnimationClipPlayable> _playables = new List<AnimationClipPlayable>();
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _body;
        private AnimationLayerMixerPlayable _layers;
        private AnimationClipPlayable _upper;

        /// <summary>Il modello copiato, figlio della comparsa.</summary>
        public Animator Model => _animator;

        /// <summary>
        /// Copia il modello animato di <paramref name="source"/> in una comparsa nuova, figlia di
        /// <paramref name="parent"/>, con la stessa scala. La copia nasce spenta e perde script, collider,
        /// suoni e interfacce prima di accendersi: niente del gioco si sveglia con lei.
        /// </summary>
        public static FilmActor Clone(GameObject source, Transform parent, string name, params AnimationClip[] clips)
        {
            var original = source.GetComponentInChildren<Animator>(true);
            var root = new GameObject(name);
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            var model = Instantiate(original.gameObject, root.transform);
            model.name = original.name;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = original.transform.lossyScale;
            Strip(model);

            var actor = root.AddComponent<FilmActor>();
            actor._animator = model.GetComponent<Animator>();
            actor._animator.runtimeAnimatorController = null;
            actor._animator.applyRootMotion = false;
            actor._animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor._clips.AddRange(clips);
            root.SetActive(true);
            return actor;
        }

        /// <summary>
        /// Tiene <paramref name="hold"/> dal petto in su e <paramref name="prop"/> nell'osso
        /// <paramref name="slot"/> della mano.
        /// </summary>
        public void Carry(AnimationClip hold, Transform prop, string slot)
        {
            Build();
            _upper = AnimationClipPlayable.Create(_graph, hold);
            _graph.Connect(_upper, 0, _layers, 1);
            _layers.SetInputWeight(1, 1f);
            var mask = new AvatarMask();
            mask.AddTransformPath(_animator.transform, true);
            for (int i = 0; i < mask.transformCount; i++)
            {
                mask.SetTransformActive(i, mask.GetTransformPath(i).Contains(UpperBody));
            }

            _layers.SetLayerMaskFromAvatarMask(1, mask);
            var hand = Find(_animator.transform, slot);
            prop.SetParent(hand != null ? hand : transform, false);
            prop.localPosition = Vector3.zero;
            prop.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// La posa al secondo <paramref name="time"/> di <paramref name="clip"/>, sfumata in
        /// <paramref name="next"/> al secondo <paramref name="nextTime"/> con il peso
        /// <paramref name="blend"/>. Le clip in loop ricominciano, le altre restano sull'ultimo fotogramma.
        /// </summary>
        public void Pose(AnimationClip clip, float time, AnimationClip next = null, float nextTime = 0f, float blend = 0f)
        {
            Build();
            blend = Mathf.Clamp01(blend);
            for (int i = 0; i < _playables.Count; i++)
            {
                float weight = 0f;
                if (_clips[i] == clip)
                {
                    weight += next != null ? 1f - blend : 1f;
                    _playables[i].SetTime(Clamp(clip, time));
                }

                if (next != null && _clips[i] == next)
                {
                    weight += blend;
                    _playables[i].SetTime(Clamp(next, nextTime));
                }

                _body.SetInputWeight(i, weight);
            }

            if (_upper.IsValid())
            {
                _upper.SetTime(time);
            }

            _graph.Evaluate(0f);
        }

        /// <summary>Dove sta e dove guarda, sul piano.</summary>
        public void Place(Vector3 position, Vector3 facing)
        {
            transform.position = position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.LookRotation(facing);
            }
        }

        public void Show(bool shown)
        {
            if (gameObject.activeSelf != shown)
            {
                gameObject.SetActive(shown);
            }
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void Build()
        {
            if (_graph.IsValid())
            {
                return;
            }

            _graph = PlayableGraph.Create(name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _layers = AnimationLayerMixerPlayable.Create(_graph, 2);
            _body = AnimationMixerPlayable.Create(_graph, _clips.Count);
            foreach (var clip in _clips)
            {
                var playable = AnimationClipPlayable.Create(_graph, clip);
                playable.SetApplyFootIK(false);
                _graph.Connect(playable, 0, _body, _playables.Count);
                _playables.Add(playable);
            }

            _graph.Connect(_body, 0, _layers, 0);
            _layers.SetInputWeight(0, 1f);
            AnimationPlayableOutput.Create(_graph, "Film", _animator).SetSourcePlayable(_layers);
        }

        private static double Clamp(AnimationClip clip, float time)
        {
            return clip.isLooping ? Mathf.Max(0f, time) : Mathf.Clamp(time, 0f, clip.length);
        }

        // gli script per primi, al contrario: chi ne richiede un altro viene dopo di lui
        private static void Strip(GameObject model)
        {
            foreach (var canvas in model.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas != null)
                {
                    DestroyImmediate(canvas.gameObject);
                }
            }

            var scripts = model.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = scripts.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(scripts[i]);
            }

            foreach (var part in model.GetComponentsInChildren<Component>(true))
            {
                if (part is Collider || part is Rigidbody || part is AudioSource || part is UnityEngine.AI.NavMeshAgent || part is UnityEngine.AI.NavMeshObstacle)
                {
                    DestroyImmediate(part);
                }
            }
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
