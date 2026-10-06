using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I numeri delle caverne (D1 della scheda M7): misure e contenuto come la cripta, più quelli del
    /// random walk. Le stesse impostazioni dicono quale generatore usare.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Cave Settings", fileName = "CaveSettings")]
    public class CaveSettings : DungeonSettings
    {
        [Header("Random walk (D1 della M7)")]
        [Tooltip("Il camminare si ferma quando il pavimento è questa frazione dell'area dentro il bordo.")]
        [SerializeField, Range(0.2f, 0.6f)] private float _floorFraction = 0.37f;

        [Tooltip("Quanti camminatori partono dal centro, scavando a turno.")]
        [SerializeField, Min(1)] private int _walkers = 4;

        [Tooltip("Probabilità a ogni passo di tenere la direzione di prima: cunicoli invece di una macchia unica.")]
        [SerializeField, Range(0f, 0.95f)] private float _straightChance = 0.65f;

        [Tooltip("Probabilità a ogni passo che un camminatore riparta da una cella già scavata: niente caverne a serpente.")]
        [SerializeField, Range(0f, 0.2f)] private float _jumpChance = 0.02f;

        [Tooltip("Probabilità a ogni passo di scavare un quadrato di 2 × 2 invece di una cella: passaggi più larghi.")]
        [SerializeField, Range(0f, 1f)] private float _wideChance = 0.15f;

        [Tooltip("Passate di smussatura: roccia isolata riempita, punte di pavimento tolte.")]
        [SerializeField, Min(0)] private int _smoothPasses = 2;

        [Header("Contenuto delle caverne (7.2)")]
        [Tooltip("Una candela a terra ogni tante celle di pavimento: niente torce nelle caverne (D4).")]
        [SerializeField, Min(4)] private int _floorPerCandle = 14;

        [Tooltip("Tra due candele almeno tante celle, in ogni direzione.")]
        [SerializeField, Min(1)] private int _candleSpacing = 3;

        [Tooltip("Un mucchio di sassi, una colonna o un tavolo rotto ogni tante celle di pavimento.")]
        [SerializeField, Min(4)] private int _floorPerProp = 30;

        [Tooltip("Nessuno scheletro e nessuna cassa a meno di tanti passi dall'ingresso.")]
        [SerializeField, Min(1)] private int _quietSteps = 6;

        public int FloorPerCandle => _floorPerCandle;

        public int CandleSpacing => _candleSpacing;

        public int FloorPerProp => _floorPerProp;

        public int QuietSteps => _quietSteps;

        public float FloorFraction => _floorFraction;

        public int Walkers => _walkers;

        public float StraightChance => _straightChance;

        public float JumpChance => _jumpChance;

        public float WideChance => _wideChance;

        public int SmoothPasses => _smoothPasses;

        public override ILevelGenerator CreateGenerator()
        {
            return new CaveGenerator(this);
        }
    }
}
