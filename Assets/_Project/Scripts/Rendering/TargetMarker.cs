using DarkDescent.Combat;
using DarkDescent.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il cerchio rosso a terra sotto il nemico che ha il cursore addosso (prova della M7): nello
    /// sciame fitto si vede quale si colpirà. Sta in Core e segue il nemico finché è vivo e sotto il
    /// cursore; non gli si appende come figlio, così quando il livello se ne va il cerchio resta.
    /// Un cerchio di LineRenderer con il materiale del settore del bruto, che si legge nel buio.
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

        private LineRenderer _line;
        private PlayerController _controller;
        private Health _target;
        private bool _subscribed;

        /// <summary>Il nemico segnato, per i test; null se il cerchio è spento.</summary>
        public Health Target => _target;

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
            _subscribed = true;
            Mark(_controller.HoveredEnemy);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _controller.HoveredEnemyChanged -= Mark;
            _subscribed = false;
            Mark(null);
        }

        private void Mark(Health enemy)
        {
            if (!ReferenceEquals(_target, null))
            {
                _target.Died -= Clear;
            }

            _target = enemy != null && !enemy.IsDead ? enemy : null;
            _line.enabled = _target != null;
            if (_target == null)
            {
                return;
            }

            _target.Died += Clear;
            float radius = (_target.TryGetComponent(out NavMeshAgent agent) ? agent.radius * _target.transform.lossyScale.x : 0.4f) + _margin;
            transform.localScale = new Vector3(radius, radius, 1f);
            Follow();
        }

        private void Clear()
        {
            Mark(null);
        }

        // segue il nemico che si muove: un solo transform, solo mentre il cerchio è acceso
        private void LateUpdate()
        {
            if (_target != null)
            {
                Follow();
            }
        }

        private void Follow()
        {
            Vector3 position = _target.transform.position;
            transform.position = new Vector3(position.x, position.y + _height, position.z);
        }
    }
}
