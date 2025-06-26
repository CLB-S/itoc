using Godot;
using ITOC.Core.Engine;

namespace ITOC;

public partial class SceneSwitcherNode : Node
{
    public SceneSwitcher SceneSwitcher { get; private set; }

    public override void _Ready()
    {
        SceneSwitcher = new SceneSwitcher(this);
        SceneSwitcher.OnReady();
    }
}
