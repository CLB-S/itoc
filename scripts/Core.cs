using Godot;

public partial class Core : Node
{
    public Settings Settings { get; private set; } = new Settings();
    public static Core Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Seed(1212);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            QuitGame();
    }

    public void QuitGame()
    {
        GD.Print("Quitting game.");
        GetTree().Quit();
    }
}