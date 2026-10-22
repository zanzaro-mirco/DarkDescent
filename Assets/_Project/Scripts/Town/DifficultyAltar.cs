using System;
using DarkDescent.Interaction;
using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// L'altare dell'accampamento (D3 della M14): l'altare della cappella con un cristallo grande, del
    /// colore della difficoltà. Raggiunto con un click, annuncia che vuole aprire il pannello delle
    /// difficoltà; pannello e scelta li gestisce il composition root.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class DifficultyAltar : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Il cristallo, tinto del colore della difficoltà con un MaterialPropertyBlock.")]
        [SerializeField] private Renderer _crystal;

        [SerializeField] private Light _light;

        [Tooltip("Quanto brilla il cristallo rispetto al suo colore.")]
        [SerializeField, Min(0f)] private float _glow = 1.2f;

        private Interactable _interactable;
        private MaterialPropertyBlock _block;

        /// <summary>Il cavaliere l'ha raggiunto: si apre il pannello.</summary>
        public event Action Opened;

        /// <summary>Il colore di adesso, del cristallo e della luce.</summary>
        public Color Color { get; private set; } = Color.white;

        /// <summary>Il cristallo e la luce prendono il colore della difficoltà.</summary>
        public void ShowColor(Color color)
        {
            Color = color;
            if (_crystal != null)
            {
                _block ??= new MaterialPropertyBlock();
                _crystal.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(EmissionColorId, color * _glow);
                _crystal.SetPropertyBlock(_block);
            }

            if (_light != null)
            {
                _light.color = color;
            }
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
        }

        private void HandleUsed(GameObject user)
        {
            Opened?.Invoke();
        }
    }
}
