using System;
using DarkDescent.Player;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Uscita verso un altro livello: quando il player entra nel suo trigger, lo annuncia. Chi decide
    /// se e come cambiare livello è il LevelManager, attraverso il LevelContext.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private string _targetScene;
        [SerializeField] private string _targetEntrance = "FromAbove";

        public event Action<LevelExit> Triggered;

        public string TargetScene => _targetScene;

        public string TargetEntrance => _targetEntrance;

        /// <summary>Per chi costruisce il livello (<see cref="LevelBuilder"/>).</summary>
        public void Configure(string targetScene, string targetEntrance)
        {
            _targetScene = targetScene;
            _targetEntrance = targetEntrance;
        }

        private void Reset()
        {
            // il player si muove con l'agent, senza Rigidbody: i messaggi dei trigger arrivano solo se
            // ce n'è uno almeno da una parte, cinematico per non cadere
            GetComponent<Collider>().isTrigger = true;
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                Triggered?.Invoke(this);
            }
        }
    }
}
