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

        [Tooltip("La profondità del livello di arrivo, per i livelli generati; 0 è il borgo. Meno di 0 = quella di questo livello più uno (M22).")]
        [SerializeField, Min(-1)] private int _targetDepth = -1;

        [Tooltip("La scala in su (M22) sta nella stanza d'ingresso, dove si cammina: non scatta entrandoci, ma solo dopo un click sopra, quando il cavaliere arriva davanti.")]
        [SerializeField] private bool _clickOnly;

        public event Action<LevelExit> Triggered;

        public string TargetScene => _targetScene;

        public string TargetEntrance => _targetEntrance;

        public int TargetDepth => _targetDepth;

        /// <summary>Scatta solo dopo un click (<see cref="Activate"/>), non entrando nel trigger.</summary>
        public bool ClickOnly => _clickOnly;

        /// <summary>Per chi costruisce il livello (<see cref="LevelBuilder"/>).</summary>
        public void Configure(string targetScene, string targetEntrance, int targetDepth, bool clickOnly = false)
        {
            _clickOnly = clickOnly;
            _targetScene = targetScene;
            _targetEntrance = targetEntrance;
            _targetDepth = targetDepth;
        }

        /// <summary>Il cavaliere è arrivato davanti all'uscita dopo un click: chi la usa dice che è scattata.</summary>
        public void Activate()
        {
            Triggered?.Invoke(this);
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
            if (!_clickOnly && other.GetComponentInParent<PlayerController>() != null)
            {
                Triggered?.Invoke(this);
            }
        }
    }
}
