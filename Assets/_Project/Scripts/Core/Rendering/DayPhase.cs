namespace DarkDescent.Rendering
{
    /// <summary>Dove sta il giorno: tra due momenti, con quanto del secondo (0 tutto il primo, 1 tutto il secondo).</summary>
    public readonly struct DayPhase
    {
        public DayPhase(DayMoment from, DayMoment to, float blend)
        {
            From = from;
            To = to;
            Blend = blend;
        }

        public DayMoment From { get; }

        public DayMoment To { get; }

        public float Blend { get; }
    }
}
