using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il click dei menu (D5 della M11): ogni pulsante sotto le radici date suona quando si preme. I
    /// pulsanti stanno tutti nella scena Core e si raccolgono una volta, all'avvio, compresi quelli dei
    /// pannelli spenti. Le etichette degli oggetti a terra no: raccoglierli ha già i suoi suoni.
    /// </summary>
    [DisallowMultipleComponent]
    public class UiClickSound : MonoBehaviour
    {
        [Tooltip("Le tele dei menu: i loro pulsanti, anche quelli spenti, fanno click.")]
        [SerializeField] private Transform[] _roots;

        [Tooltip("Sorgente 2D sul gruppo UI del mixer.")]
        [SerializeField] private AudioSource _audio;

        [SerializeField] private AudioClip _click;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;

        private readonly List<Button> _buttons = new List<Button>();

        /// <summary>Quanti pulsanti fanno click: per i test.</summary>
        public int Count => _buttons.Count;

        /// <summary>Il click è partito: per i test.</summary>
        public event System.Action Played;

        private void Awake()
        {
            // il menu di pausa ferma l'audio del mondo, non i suoi pulsanti
            if (_audio != null)
            {
                _audio.ignoreListenerPause = true;
            }

            foreach (var root in _roots)
            {
                if (root == null)
                {
                    continue;
                }

                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponentInParent<GroundLabel>(true) != null)
                    {
                        continue;
                    }

                    button.onClick.AddListener(Play);
                    _buttons.Add(button);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var button in _buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(Play);
                }
            }

            _buttons.Clear();
        }

        private void Play()
        {
            if (_audio != null && _click != null)
            {
                _audio.PlayOneShot(_click, _volume);
            }

            Played?.Invoke();
        }
    }
}
