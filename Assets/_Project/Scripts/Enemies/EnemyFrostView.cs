using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il gelo di un nemico che si vede (M21): una tinta fredda mentre è rallentato, più forte e con
    /// l'animazione ferma mentre è congelato. Nasce da solo al primo gelo (<see cref="EnemyAI"/>): niente
    /// nei prefab. Rimette la tinta di prima, quella dei campioni compresa, e la velocità dell'animazione.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyFrostView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color SlowTint = new Color(0.65f, 0.85f, 1f, 1f);
        private static readonly Color FreezeTint = new Color(0.4f, 0.7f, 1f, 1f);

        private EnemyAI _ai;
        private Animator _animator;
        private Renderer[] _renderers;
        private Color[] _original;
        private MaterialPropertyBlock _block;
        private float _animatorSpeed = 1f;
        private bool _tinted;
        private bool _paused;

        /// <summary>La tinta che porta adesso: bianca se non ha niente.</summary>
        public Color Tint => _tinted ? (_ai.IsFrozen ? FreezeTint : SlowTint) : Color.white;

        public bool IsPaused => _paused;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _animator = GetComponentInChildren<Animator>();
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _ai.Frozen += HandleChanged;
            _ai.FreezeEnded += HandleEnded;
            _ai.SlowChanged += HandleEnded;
            Refresh();
        }

        private void OnDisable()
        {
            _ai.Frozen -= HandleChanged;
            _ai.FreezeEnded -= HandleEnded;
            _ai.SlowChanged -= HandleEnded;
            Restore();
        }

        private void HandleChanged(EnemyAI ai, float seconds)
        {
            Refresh();
        }

        private void HandleEnded(EnemyAI ai)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool frozen = _ai.IsFrozen;
            if (!frozen && !_ai.IsSlowed)
            {
                Restore();
                return;
            }

            if (!_tinted)
            {
                // i pezzi cambiano da un nemico all'altro: si prendono quando serve, non a ogni frame
                _renderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r is MeshRenderer || r is SkinnedMeshRenderer);
                _original = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].GetPropertyBlock(_block);
                    _original[i] = _block.HasColor(BaseColorId) ? _block.GetColor(BaseColorId) : Color.white;
                }

                _tinted = true;
            }

            SetTint(frozen ? FreezeTint : SlowTint, true);
            if (frozen && !_paused && _animator != null)
            {
                _animatorSpeed = _animator.speed;
                _animator.speed = 0f;
                _paused = true;
            }
            else if (!frozen && _paused)
            {
                Unpause();
            }
        }

        private void Restore()
        {
            if (_paused)
            {
                Unpause();
            }

            if (!_tinted)
            {
                return;
            }

            SetTint(Color.white, false);
            _tinted = false;
        }

        private void Unpause()
        {
            if (_animator != null)
            {
                _animator.speed = _animatorSpeed;
            }

            _paused = false;
        }

        // moltiplica la tinta che c'era: un campione azzurrato resta se stesso, solo più freddo
        private void SetTint(Color tint, bool freezing)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                {
                    continue;
                }

                _renderers[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, freezing ? _original[i] * tint : _original[i]);
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
