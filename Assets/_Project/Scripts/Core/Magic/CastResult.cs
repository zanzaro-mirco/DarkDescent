namespace DarkDescent.Magic
{
    /// <summary>Se un incantesimo può partire, o perché no (D4 della M9: il posto si scurisce e suona sordo).</summary>
    public enum CastResult
    {
        Ready,
        NotKnown,
        NoMana,
        Cooldown,
    }
}
