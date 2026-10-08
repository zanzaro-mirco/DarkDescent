using DarkDescent.Localization;
using DarkDescent.Progression;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// La barra dell'esperienza in basso al centro, con il livello sopra (passo 8.2 della M8): si
    /// riempie verso il livello successivo, piena al livello massimo. Ascolta la crescita del
    /// cavaliere e la lingua, mai in Update.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExperienceBar : MonoBehaviour
    {
        [Tooltip("La parte piena: si allunga spostando il suo bordo destro, senza sprite.")]
        [SerializeField] private RectTransform _fill;

        [SerializeField] private TMP_Text _label;

        private CharacterProgress _progress;
        private Localizer _localizer;
        private bool _subscribed;

        public float FillAmount => _fill.anchorMax.x;

        public string LabelText => _label.text;

        /// <summary>Come i pannelli: Bind e OnEnable in ordine qualsiasi, si iscrive chi arriva per secondo.</summary>
        public void Bind(CharacterProgress progress, Localizer localizer)
        {
            Unsubscribe();
            _progress = progress;
            _localizer = localizer;
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
            if (_subscribed || _progress == null)
            {
                return;
            }

            _progress.Changed += Refresh;
            _localizer.LanguageChanged += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _progress.Changed -= Refresh;
            _localizer.LanguageChanged -= Refresh;
            _subscribed = false;
        }

        private void Refresh()
        {
            int next = _progress.ExperienceToNext;
            float fill = next > 0 ? Mathf.Clamp01((float)_progress.Experience / next) : 1f;
            _fill.anchorMax = new Vector2(fill, 1f);
            _label.text = next > 0
                ? _localizer.Format(TextKeys.ExperienceLevel, _progress.Level, _progress.Experience, next)
                : _localizer.Format(TextKeys.ExperienceMaxLevel, _progress.Level);
        }
    }
}
