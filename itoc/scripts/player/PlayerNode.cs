using Godot;
using ITOC.Core.Entity;

namespace ITOC;

public partial class PlayerNode : CharacterBody3D
{
    public Player Player { get; private set; }

    public override void _Ready()
    {
        Player = new Player(this);

        Player.OnReady();
    }

    public override void _Input(InputEvent inputEvent) => Player.OnInput(inputEvent);

    public override void _PhysicsProcess(double delta) => Player.OnPhysicsProcess(delta);
}
