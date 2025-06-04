using Godot;
using System;

public partial class GuiTest : Control
{
    public void OnButtonPressed()
    {
        var newTexture = ResourceLoader.Load<Texture2D>("res://assets/gui/itoc_gui_dark.svg");

        var theme = ThemeDB.GetProjectTheme();
        foreach (var type in theme.GetStyleboxTypeList())
            foreach (var styleBoxName in theme.GetStyleboxList(type))
            {
                var styleBox = theme.GetStylebox(styleBoxName, type);

                if (styleBox is StyleBoxTexture styleBoxTexture)
                {
                    styleBoxTexture.Texture = newTexture;
                    GD.Print($"Updated StyleBox: {type} - {styleBoxName}");
                }
            }
    }
}