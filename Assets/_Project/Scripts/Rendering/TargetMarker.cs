using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il cerchio rosso a terra sotto il nemico che ha il cursore addosso (prova della M7): nello
    /// sciame fitto si vede quale si colpirà. Sta in Core e segue il nemico finché è vivo e sotto il
    /// cursore; non gli si appende come figlio, così quando il livello se ne va il cerchio resta.
    /// Un cerchio di LineRenderer con il materiale del settore del bruto, che si legge nel buio. Dalla
    /// M13 segna anche il barile da rompere sotto il cursore (D5), che si clicca come un nemico.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public class TargetMarker : MonoBehaviour
    {
        [Tooltip("Quanto il cerchio è più largo del nemico, in metri.")]
        [SerializeField, Min(0f)] private float _margin = 0.15f;

        [SerializeField, Range(8, 64)] private int _segments = 32;

        [Tooltip("Altezza da terra: sopra il pavimento, sotto i piedi.")]
        [SerializeField, Min(0f)] private float _height = 0.04f;

        [Tooltip("Altezza sotto un barile: il barile parte dal fondo della piastrella, 5 cm sotto il NavMesh dei piedi.")]
        [SerializeField, Min(0f)] private float _barrelHeight = 0.1f;

        private LineRenderer _line;
        private PlayerController _controller;
        private Health _target;
        private Breakable _barrel;
        private Transform _followed;
        private float _lift;
        private bool _subscribed;

        /// <summary>Il nemico segnato, per i test; null se il cerchio è spento o segna un barile.</summary>
        public Health Target => _target;

        /// <summary>Il barile segnato, per i test; null se il cerchio è spento o segna un nemico.</summary>
        public Breakable BarrelTarget => _barrel;

        private void Awake()
        {
            // un cerchio di raggio 1 nel piano XY locale, steso a terra dalla rotazione di 90° su X e
            // girato verso l'alto da TransformZ: la scala lo adatta al nemico
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.alignment = LineAlignment.TransformZ;
            _line.positionCount = _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = i * Mathf.PI * 2f / _segments;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }

            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _line.enabled = false;
        }

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerController controller)
        {
            Unsubscribe();
            _controller = controller;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _controller == null)
            {
                return;
            }

            _controller.HoveredEnemyChanged += Mark;
            _controller.HoveredBreakableChanged += MarkBarrel;
            _subscribed = true;
            Mark(_controller.HoveredEnemy);
            MarkBarrel(_controller.HoveredBreakable);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _controller.HoveredEnemyChanged -= Mark;
            _controller.HoveredBreakableChanged -= MarkBarrel;
            _subscribed = false;
            Detach();
        }

        // Nemico e barile sotto il cursore arrivano con due eventi: il null dell'uno non spegne il cerchio
        // dell'altro, che il controller ha appena acceso
        private void Mark(Health enemy)
        {
            if (enemy == null && ReferenceEquals(_target, null))
            {
                return;
            }

            Detach();
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            _target = enemy;
            _target.Died += Clear;
            Show(enemy.transform, enemy.TryGetComponent(out NavMeshAgent agent) ? agent.radius * enemy.transform.lossyScale.x : 0.4f, _height);
        }

        private void MarkBarrel(Breakable barrel)
        {
            if (barrel == null && ReferenceEquals(_barrel, null))
            {
                return;
            }

            Detach();
            if (barrel == null || barrel.IsBroken)
            {
                return;
            }

            _barrel = barrel;
            _barrel.Broken += ClearBarrel;
            Show(barrel.transform, barrel.Radius, _barrelHeight);
        }

        private void Show(Transform followed, float radius, float lift)
        {
            _followed = followed;
            _lift = lift;
            radius += _margin;
            transform.localScale = new Vector3(radius, radius, 1f);
            _line.enabled = true;
            Follow();
        }

        // il nemico o il barile di prima possono essere già distrutti con il loro livello: ci si stacca lo stesso
        private void Detach()
        {
            if (!ReferenceEquals(_target, null))
            {
                _target.Died -= Clear;
            }

            if (!ReferenceEquals(_barrel, null))
            {
                _barrel.Broken -= ClearBarrel;
            }

            _target = null;
            _barrel = null;
            _followed = null;
            _line.enabled = false;
        }

        private void Clear()
        {
            Detach();
        }

        private void ClearBarrel(Breakable barrel)
        {
            Detach();
        }

        // segue il nemico che si muove: un solo transform, solo mentre il cerchio è acceso
        private void LateUpdate()
        {
            if (_followed != null)
            {
                Follow();
            }
        }

        private void Follow()
        {
            Vector3 position = _followed.position;
            transform.position = new Vector3(position.x, position.y + _lift, position.z);
        }
    }
}
