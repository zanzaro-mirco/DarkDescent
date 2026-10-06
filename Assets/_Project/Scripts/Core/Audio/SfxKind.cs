namespace DarkDescent.Audio
{
    /// <summary>
    /// I tipi di suono dei personaggi (D9 della M7): due suoni dello stesso tipo nello stesso
    /// fotogramma ne fanno sentire uno solo. I valori fanno da indice: nuove voci in fondo.
    /// </summary>
    public enum SfxKind
    {
        Swing,
        Hit,
        Death,
        Block,
        Heal,
        CriticalVoice,
    }
}
