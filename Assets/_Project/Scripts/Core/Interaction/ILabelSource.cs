using DarkDescent.Localization;

namespace DarkDescent.Interaction
{
    /// <summary>Chi compone da sé il nome di un <see cref="Interactable"/>, nella lingua attiva.</summary>
    public interface ILabelSource
    {
        string GetLabel(Localizer localizer);
    }
}
