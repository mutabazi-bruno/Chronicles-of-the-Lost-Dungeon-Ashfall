namespace Ashfall.Interfaces
{
    // stuff the player can activate - doors, switches, chests
    public interface IInteractable
    {
        void Interact();

        // What to show on screen while the player is standing next to this thing.
        // An empty string means show nothing, which is what a chest that has already been
        // looted should do.
        string InteractionPrompt { get; }

        // False means pressing interact does nothing right now. A locked door still returns
        // a prompt in that case, because "Locked. You need the Rusty Key" is more use to the
        // player than silence.
        bool CanInteract { get; }
    }
}
