using Engine.Drawing;
using Optional;

namespace Engine
{
    public interface IState
    {
        // Handle keyboard inputs.
        Option<ICommand> HandleKeyInput(int key);

        // Handle mouse inputs.
        Option<ICommand> HandleMouseInput(double x, double y, ClickState click);

        // Update state information.
        // void Update(ICommand command);

        // Draw to the screen.
        void Draw(LayerInfo layer);
    }

    public enum ClickState
    {
        None, Left, Right, X1, X2
    }
}
