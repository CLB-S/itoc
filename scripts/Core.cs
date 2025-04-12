using Godot;

public partial class Core : Node
{
    public Settings Settings { get; private set; }
    public static Core Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Seed(1212);

        // Load settings
        Settings = Settings.Load();
        Settings.ApplyGraphicsSettings();
        GetWindow().MoveToCenter();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            QuitGame();
    }

    public void QuitGame()
    {
        // Save settings on quit
        Settings.Save();
        GD.Print("Quitting game.");
        GetTree().Quit();
    }
}
