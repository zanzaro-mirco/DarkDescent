using DarkDescent.UI;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I passi del menu che molti test rifanno: una partita nuova in un posto, con la classe scelta nella
    /// creazione del personaggio (D8 della M21). Anche le foto li usano.
    /// </summary>
    public static class MenuSteps
    {
        /// <summary>
        /// Nuova partita, il posto (libero), la classe e Comincia, con il nome di partenza della classe. Il
        /// menu deve essere aperto sul menu principale.
        /// </summary>
        public static void StartNew(MainMenu menu, int slot, string classId = "warrior", string heroName = null)
        {
            menu.NewGameButton.onClick.Invoke();
            menu.SlotView(slot).Button.onClick.Invoke();
            Create(menu, classId, heroName);
        }

        /// <summary>La creazione già aperta (dopo il posto e, su uno occupato, la conferma): la classe, il nome se c'è, Comincia.</summary>
        public static void Create(MainMenu menu, string classId, string heroName = null)
        {
            Assert.IsTrue(menu.IsCreating, "dopo il posto si apre la creazione del personaggio");
            menu.Creation.Card(classId).onClick.Invoke();
            if (heroName != null)
            {
                menu.Creation.NameField.text = heroName;
            }

            menu.Creation.StartButton.onClick.Invoke();
        }
    }
}
