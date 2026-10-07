using System;
using System.Collections.Generic;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Esegue uno stato alla volta per un corpo (D8 della M7). Gli stati li sceglie l'archetipo; il
    /// cervello li conosce per <see cref="EnemyState"/> e passa da uno all'altro quando uno lo chiede.
    /// Logica pura: niente Unity oltre al tempo che gli viene passato.
    /// </summary>
    public sealed class EnemyBrain
    {
        private readonly IEnemyBody _body;
        private readonly Dictionary<EnemyState, EnemyStateBase> _states = new Dictionary<EnemyState, EnemyStateBase>();
        private EnemyStateBase _current;

        public EnemyBrain(IEnemyBody body, IEnumerable<EnemyStateBase> states)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            foreach (var state in states)
            {
                _states[state.Id] = state;
            }

            if (!_states.ContainsKey(EnemyState.Idle) || !_states.ContainsKey(EnemyState.Dead))
            {
                throw new ArgumentException("Servono almeno gli stati Idle e Dead.", nameof(states));
            }

            _current = _states[EnemyState.Idle];
        }

        /// <summary>Lo stato di prima e quello nuovo, quando cambia: per animazioni e suoni.</summary>
        public event Action<EnemyState, EnemyState> StateChanged;

        public EnemyState State => _current.Id;

        public void Tick(float deltaTime)
        {
            EnemyState next = _current.Tick(_body, deltaTime);
            if (next != _current.Id)
            {
                Switch(next);
            }
        }

        /// <summary>
        /// Un compagno ha visto il bersaglio (lo sciame, D5): chi è fermo comincia a inseguire senza
        /// averlo visto. Negli altri stati, o con il bersaglio già morto, non cambia niente.
        /// </summary>
        public bool Alert()
        {
            if (_current.Id != EnemyState.Idle || !_body.IsTargetAlive || !_states.ContainsKey(EnemyState.Chase))
            {
                return false;
            }

            Switch(EnemyState.Chase);
            return true;
        }

        /// <summary>
        /// Torna fermo da qualsiasi stato, lasciando il bersaglio: il cavaliere è tornato in vita
        /// all'ingresso (D15 della M7). Da morto non cambia niente.
        /// </summary>
        public bool Rest()
        {
            if (_current.Id == EnemyState.Dead)
            {
                return false;
            }

            _body.Disengage();
            Switch(EnemyState.Idle);
            return true;
        }

        /// <summary>La morte arriva da fuori (la vita), non da uno stato: vale subito, da qualsiasi stato.</summary>
        public void Die()
        {
            if (_current.Id != EnemyState.Dead)
            {
                Switch(EnemyState.Dead);
            }
        }

        private void Switch(EnemyState next)
        {
            if (!_states.TryGetValue(next, out var state))
            {
                throw new InvalidOperationException($"Lo stato {_current.Id} chiede {next}, che questo nemico non ha.");
            }

            EnemyState previous = _current.Id;
            _current = state;
            state.Enter(_body);
            StateChanged?.Invoke(previous, next);
        }
    }
}
