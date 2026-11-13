namespace BreakingPizza.Interaction
{
    // Interfaz de los objetos con los que se puede interactuar.
    public interface IInteractable
    {
        void Interact();

        string GetPrompt();
    }
}
