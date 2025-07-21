using Godot;

namespace ITOC.Core;

public interface IRenderingModel
{
    Control GetGuiControl();
    Node3D Get3dModel();
    Node3D Get3dModelInHand();
}
