using System;
using DarkDescent.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Le opzioni (D10 della M10), dal menu iniziale e dalla pausa: la lingua, che sostituisce il
    /// tasto F9 della M5, i volumi generale, della musica e degli effetti, e i nomi degli oggetti a
    /// terra sempre visibili. Tutto resta tra le preferenze, come zoom e lingua.
    /// </summary>
    [DisallowMultipleComponent]
    public class OptionsPanel : MonoBehaviour
    {
        public const string MasterPreference = "volume.master";
        public const string MusicPreference = "volume.music";
        public const string SfxPreference = "volume.sfx";
        public const string LabelsPreference = "labels.always";

        // i parametri esposti del mixer Main
        public const string MasterParameter = "MasterVolume";
        public const string MusicParameter = "MusicVolume";
        public const string SfxParameter = "SfxVolume";

        [Tooltip("Il riquadro da mostrare; il componente sta sul padre, sempre acceso.")]
        [SerializeField] private GameObject _root;

        [SerializeField] private Button _language;

        [SerializeField] private TMP_Text _languageLabel;

        [SerializeField] private Slider _master;

        [SerializeField] private Slider _music;

        [SerializeField] private Slider _sfx;

        [SerializeField] private Toggle _labels;

        [SerializeField] private Button _back;

        [SerializeField] private AudioMixer _mixer;

        private Localizer _localizer;
        private GroundLabels _groundLabels;
        private bool _subscribed;

        /// <summary>Il tasto della lingua: chi tiene le preferenze cambia lingua e la ricorda.</summary>
        public event Action LanguageCycleRequested;

        /// <summary>Chiuse con Indietro: chi le ha aperte torna a mostrarsi.</summary>
        public event Action Closed;

        public bool IsOpen => _root.activeSelf;

        public Button LanguageButton => _language;

        public Slider Master => _master;

        public Toggle Labels => _labels;

        public Button Back => _back;

        public string LanguageText => _languageLabel.text;

        public void Bind(Localizer localizer, GroundLabels groundLabels)
        {
            Unsubscribe();
            _localizer = localizer;
            _groundLabels = groundLabels;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Open()
        {
            _root.SetActive(true);
            RefreshLanguage();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _root.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>Un volume tra 0 e 1 in decibel: lineare per l'orecchio, −80 dB per il silenzio.</summary>
        public static float ToDecibels(float volume)
        {
            return volume <= 0.0001f ? -80f : 20f * Mathf.Log10(volume);
        }

        private void Awake()
        {
            _root.SetActive(false);
            _master.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterPreference, 1f));
            _music.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicPreference, 1f));
            _sfx.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxPreference, 1f));
            _labels.SetIsOnWithoutNotify(PlayerPrefs.GetInt(LabelsPreference, 0) == 1);
        }

        // il mixer non accetta valori in Awake: i volumi salvati si applicano qui
        private void Start()
        {
            ApplyVolumes();
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
            if (_subscribed || _localizer == null)
            {
                return;
            }

            _language.onClick.AddListener(HandleLanguage);
            _back.onClick.AddListener(Close);
            _master.onValueChanged.AddListener(HandleVolume);
            _music.onValueChanged.AddListener(HandleVolume);
            _sfx.onValueChanged.AddListener(HandleVolume);
            _labels.onValueChanged.AddListener(HandleLabels);
            _localizer.LanguageChanged += RefreshLanguage;
            _subscribed = true;
            _groundLabels.SetAlwaysVisible(_labels.isOn);
            RefreshLanguage();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _language.onClick.RemoveListener(HandleLanguage);
            _back.onClick.RemoveListener(Close);
            _master.onValueChanged.RemoveListener(HandleVolume);
            _music.onValueChanged.RemoveListener(HandleVolume);
            _sfx.onValueChanged.RemoveListener(HandleVolume);
            _labels.onValueChanged.RemoveListener(HandleLabels);
            _localizer.LanguageChanged -= RefreshLanguage;
            _subscribed = false;
        }

        private void HandleLanguage()
        {
            LanguageCycleRequested?.Invoke();
        }

        private void HandleVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterPreference, _master.value);
            PlayerPrefs.SetFloat(MusicPreference, _music.value);
            PlayerPrefs.SetFloat(SfxPreference, _sfx.value);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        private void HandleLabels(bool on)
        {
            PlayerPrefs.SetInt(LabelsPreference, on ? 1 : 0);
            PlayerPrefs.Save();
            _groundLabels.SetAlwaysVisible(on);
        }

        private void ApplyVolumes()
        {
            if (_mixer == null)
            {
                return;
            }

            _mixer.SetFloat(MasterParameter, ToDecibels(_master.value));
            _mixer.SetFloat(MusicParameter, ToDecibels(_music.value));
            _mixer.SetFloat(SfxParameter, ToDecibels(_sfx.value));
        }

        private void RefreshLanguage()
        {
            if (_localizer != null)
            {
                _languageLabel.text = _localizer.Format(TextKeys.OptionsLanguage, _localizer.Get(TextKeys.LanguageName));
            }
        }
    }
}
