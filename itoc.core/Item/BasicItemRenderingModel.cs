using Godot;

namespace ITOC.Core.Item;

public class BasicItemRenderingModel : IRenderingModel
{
    private readonly Texture2D _itemTexture;

    public BasicItemRenderingModel(Texture2D itemTexture) => _itemTexture = itemTexture;

    private Node3D GetModel(bool inHand = false)
    {
        var meshInstance = new MeshInstance3D()
        {
            Mesh = new BoxMesh(),
            Scale = new Vector3(1, 1, 0.1),
        };

        var material = new ShaderMaterial
        {
            Shader = ResourceLoader.Load<Shader>("res://assets/shaders/texture_extrude.gdshader"),
        };

        material.SetShaderParameter("Texture", _itemTexture);
        if (inHand)
            material.SetShaderParameter("texture_calls", 48);

        meshInstance.MaterialOverride = material;
        return meshInstance;
    }

    public Node3D Get3dModel() => GetModel();

    public Node3D Get3dModelInHand() => GetModel(true);

    public Control GetGuiControl() => new TextureRect { Texture = _itemTexture };
}
