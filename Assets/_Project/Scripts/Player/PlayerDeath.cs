using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Player
{
    /// <summary>
    /// Alla morte del player spegne tutto ciò che agisce: input, controller, attacco e agent.
    /// L'animazione di morte la fa partire il driver, la schermata la mostra l'HUD: qui solo il corpo.
    /// Tornato in vita (D13 della M6) riaccende input e attacco; controller e agent li riaccende il
    /// LevelManager entrando nel livello, come a ogni cambio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class PlayerDeath : MonoBehaviour
    {
        private Health _health;
        private PlayerInputReader _input;
        private PlayerController _controller;
        private PlayerMotor _motor;
        private MeleeAttack _attack;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _input = GetComponent<PlayerInputReader>();
            _controller = GetComponent<PlayerController>();
            _motor = GetComponent<PlayerMotor>();
            _attack = GetComponent<MeleeAttack>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            _health.Died += HandleDied;
            _health.Revived += HandleRevived;
        }

        private void OnDisable()
        {
            _health.Died -= HandleDied;
            _health.Revived -= HandleRevived;
        }

        private void HandleDied()
        {
            // prima il controller, poi il reader: spegnendo il reader con il tasto premuto parte un
            // canceled, e il controller non deve più essere in ascolto
            _controller.enabled = false;
            _input.enabled = false;

            // MeleeAttack spento annulla anche un fendente in volo
            _attack.enabled = false;
            _motor.Stop();

            // come per i nemici: il corpo non spinge gli altri agent
            _agent.enabled = false;
        }

        private void HandleRevived()
        {
            _input.enabled = true;
            _attack.enabled = true;
        }
    }
}
