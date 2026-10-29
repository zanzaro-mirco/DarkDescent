using System;
using DarkDescent.Combat;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Magic;
using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// Un banco del borgo (D5 della M10, D18 della M18): il mercante, il fabbro o la guaritrice.
    /// Raggiunto con un click, annuncia che vuole aprire il negozio: banco e finestra li gestisce il
    /// composition root. Quando il cliente si allontana dal banco annuncia che se n'è andato, e il
    /// negozio si chiude. Con la cura, chi arriva ha subito vita e mana pieni, gratis, come da Pepin.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Merchant : MonoBehaviour
    {
        [Tooltip("Oltre questa distanza dal punto di arrivo il cliente se n'è andato e il negozio si chiude.")]
        [SerializeField, Min(1f)] private float _leaveDistance = 3f;

        [Tooltip("Che cosa vende e compra: le provviste e gli oggetti della tabella di questi tipi.")]
        [SerializeField] private ShopGoods _goods = ShopGoods.All;

        [Tooltip("Quanti oggetti tira a ogni ritorno in città.")]
        [SerializeField, Min(0)] private int _stockSize = 8;

        [Tooltip("Riempie vita e mana di chi arriva: la guaritrice.")]
        [SerializeField] private bool _heals;

        private Interactable _interactable;
        private Transform _customer;

        /// <summary>Il cavaliere l'ha raggiunto: si apre il negozio.</summary>
        public event Action<Merchant> Opened;

        /// <summary>Il cavaliere si è allontanato dal banco: il negozio si chiude.</summary>
        public event Action<Merchant> Left;

        public float LeaveDistance => _leaveDistance;

        public ShopGoods Goods => _goods;

        public int StockSize => _stockSize;

        public bool Heals => _heals;

        /// <summary>Il nome del banco, sull'etichetta e sulla finestra.</summary>
        public string NameKey => GetComponent<Interactable>().LabelKey;

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
            _customer = null;
        }

        // solo mentre c'è un cliente: un confronto di distanze, senza allocare
        private void Update()
        {
            if (_customer == null)
            {
                return;
            }

            if ((_customer.position - _interactable.ApproachPoint).sqrMagnitude > _leaveDistance * _leaveDistance)
            {
                _customer = null;
                Left?.Invoke(this);
            }
        }

        private void HandleUsed(GameObject user)
        {
            _customer = user != null ? user.transform : null;
            if (_heals && user != null)
            {
                Heal(user);
            }

            Opened?.Invoke(this);
        }

        private static void Heal(GameObject user)
        {
            if (user.TryGetComponent(out Health health) && !health.IsDead)
            {
                health.Heal(health.Max);
            }

            if (user.TryGetComponent(out PlayerMagic magic))
            {
                magic.Mana.Fill();
            }
        }
    }
}
