using System.Collections.Generic;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// L'albero delle abilità del cavaliere (M20): crea il <see cref="TalentBuild"/> con l'albero
    /// del Guerriero e le sue due specializzazioni, gli passa il livello della crescita e mette sulla
    /// scheda i passivi applicati, con sé come sorgente, così si tolgono insieme a ogni *Applica* e
    /// all'altare. I passivi a una condizione e gli effetti al colpo arrivano con le abilità (20.4).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(PlayerProgress))]
    public class PlayerTalents : MonoBehaviour
    {
        [SerializeField] private TalentTree _classTree;

        [SerializeField] private TalentTree[] _specializations;

        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private TalentBuild _build;
        private CharacterProgress _progress;

        // creato al primo accesso, come la crescita: pannello e salvataggio si collegano dal
        // composition root, in un ordine che rispetto all'Awake non è garantito
        public TalentBuild Build => _build ??= new TalentBuild(_classTree, _specializations, Progress.Level);

        private CharacterProgress Progress => _progress ??= GetComponent<PlayerProgress>().Progress;

        private void OnEnable()
        {
            Progress.Changed += HandleProgressChanged;
            Build.Applied += Refresh;
            HandleProgressChanged();
            Refresh();
        }

        private void OnDisable()
        {
            Progress.Changed -= HandleProgressChanged;
            Build.Applied -= Refresh;
        }

        private void HandleProgressChanged()
        {
            Build.SetLevel(Progress.Level);
        }

        // i passivi applicati sulla scheda: via tutti i vecchi, dentro i nuovi
        private void Refresh()
        {
            var sheet = GetComponent<CharacterStats>().Sheet;
            sheet.RemoveModifiersFrom(this);
            _modifiers.Clear();
            Build.CollectModifiers(_modifiers, this, null);
            foreach (var modifier in _modifiers)
            {
                sheet.AddModifier(modifier);
            }
        }
    }
}
