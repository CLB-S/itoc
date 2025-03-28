using Godot;
using System;

public partial class MultimeshTest : MultiMeshInstance3D
{
    public override void _Ready()
    {
        // Create the multimesh.
        Multimesh = new MultiMesh();
        // Set the format first.
        Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        Multimesh.Mesh = new PlaneMesh()
        {
            Material = new StandardMaterial3D()
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
                TextureRepeat = true,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            }
        };
        // Then resize (otherwise, changing the format is not allowed)
        Multimesh.InstanceCount = 50000;
        // Maybe not all of them should be visible at first.
        Multimesh.VisibleInstanceCount = 50000;

        // Set the transform of the instances.
        for (int i = 0; i < Multimesh.VisibleInstanceCount; i++)
        {
            Multimesh.SetInstanceTransform(i, new Transform3D(Basis.Identity,
                new Vector3(GD.Randf() * 100, -GD.Randf() * 100, GD.Randf() * 100)));
        }
    }
}
