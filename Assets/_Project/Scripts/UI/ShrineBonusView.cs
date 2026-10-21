using DarkDescent.Interaction;
using DarkDescent.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// L'icona del bonus del santuario accanto alla cintura (D6 della M13): il cristallo del colore del
    /// santuario e i secondi che restano. Si vede solo mentre il bonus dura; si ridisegna sugli eventi
    /// del bonus, una volta al secondo.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShrineBonusView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;

        [SerializeField] private Image _icon;

        [SerializeField] private TMP_Text _seconds;

        private ShrineBonus _bonus;
        private bool _subscribed;

        public bool IsShowing => _group.alpha > 0f;

        public string SecondsText => _seconds.text;

        public Color IconColor => _icon.color;

        /// <summary>Come la cintura: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(ShrineBonus bonus)
        {
            Unsubscribe();
            _bonus = bonus;
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
            if (_subscribed || _bonus == null)
            {
                return;
            }

            _bonus.Started += Show;
            _bonus.SecondsChanged += SetSeconds;
            _bonus.Ended += Hide;
            _subscribed = true;
            if (_bonus.Current != null)
            {
                Show(_bonus.Current);
                SetSeconds(_bonus.SecondsLeft);
            }
            else
            {
                Hide(null);
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _bonus.Started -= Show;
            _bonus.SecondsChanged -= SetSeconds;
            _bonus.Ended -= Hide;
            _subscribed = false;
        }

        private void Show(ShrineDefinition shrine)
        {
            _icon.color = shrine.Color;
            _group.alpha = 1f;
        }

        private void SetSeconds(int seconds)
        {
            _seconds.text = seconds.ToString();
        }

        private void Hide(ShrineDefinition shrine)
        {
            _group.alpha = 0f;
        }
    }
}
