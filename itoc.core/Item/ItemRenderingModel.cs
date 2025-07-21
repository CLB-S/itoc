using Godot;

namespace ITOC.Core.Item;

public class ItemRenderingModel : IRenderingModel
{
    private readonly Node3D _model;
    private readonly Control _guiIcon;

    public ItemRenderingModel(Node3D model, Control guiIcon)
    {
        _model = model;
        _guiIcon = guiIcon;
    }

    public Node3D Get3dModel() => _model;

    public Node3D Get3dModelInHand() => _model;

    public Control GetGuiControl() => _guiIcon;
}
