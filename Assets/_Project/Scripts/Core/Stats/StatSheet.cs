using System;
using System.Collections.Generic;

namespace DarkDescent.Stats
{
    /// <summary>
    /// Statistiche di un personaggio, senza dipendenze da Unity. Ordine di applicazione (D2 della
    /// M4): valore = (base + somma dei fissi) × (1 + somma delle percentuali / 100). Le percentuali
    /// si sommano tra loro e non si moltiplicano: con gli affissi della M5 crescerebbero in modo
    /// esponenziale, e nel tooltip non si leggerebbero più.
    /// </summary>
    public sealed class StatSheet
    {
        private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        private readonly float[] _base = new float[StatCount];
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        /// <summary>Cambiato un valore base o un modificatore: chi mostra le statistiche le rilegge.</summary>
        public event Action Changed;

        public float GetBase(StatType stat)
        {
            return _base[(int)stat];
        }

        public void SetBase(StatType stat, float value)
        {
            _base[(int)stat] = value;
            Changed?.Invoke();
        }

        /// <summary>Il valore con tutti i modificatori applicati.</summary>
        public float Get(StatType stat)
        {
            float flat = 0f;
            float percent = 0f;
            foreach (var modifier in _modifiers)
            {
                if (modifier.Stat != stat)
                {
                    continue;
                }

                if (modifier.Kind == ModifierKind.Flat)
                {
                    flat += modifier.Value;
                }
                else
                {
                    percent += modifier.Value;
                }
            }

            return (_base[(int)stat] + flat) * (1f + percent / 100f);
        }

        public void AddModifier(in StatModifier modifier)
        {
            // senza sorgente non si potrebbe più togliere
            if (modifier.Source == null)
            {
                throw new ArgumentException("Un modificatore deve avere una sorgente", nameof(modifier));
            }

            _modifiers.Add(modifier);
            Changed?.Invoke();
        }

        /// <summary>Toglie tutti i modificatori messi da <paramref name="source"/>; restituisce quanti erano.</summary>
        public int RemoveModifiersFrom(object source)
        {
            int removed = 0;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_modifiers[i].Source, source))
                {
                    _modifiers.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                Changed?.Invoke();
            }

            return removed;
        }
    }
}
