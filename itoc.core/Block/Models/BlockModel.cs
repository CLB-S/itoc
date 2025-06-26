using Godot;

namespace ITOC.Core.BlockModels;

public abstract class BlockModelBase
{
    public Transform3D GuiTransform { get; set; }
    public Transform3D GroundTransform { get; set; }
    public Transform3D FixedDisplayTransform { get; set; }
    public Transform3D ThirdPersonRightHandTransform { get; set; }
    public Transform3D FirstPersonRightHandTransform { get; set; }
    public Transform3D FirstPersonLeftHandTransform { get; set; }

    public BlockModelBase()
    {
        GuiTransform = new Transform3D(
            Basis
                .Identity.Rotated(Vector3.Right, Mathf.DegToRad(30))
                .Rotated(Vector3.Up, Mathf.DegToRad(225))
                .Rotated(Vector3.Forward, Mathf.DegToRad(0))
                .Scaled(new Vector3(0.625f, 0.625f, 0.625f)),
            new Vector3(0, 0, 0)
        );

        GroundTransform = new Transform3D(
            Basis.Identity.Scaled(new Vector3(0.25f, 0.25f, 0.25f)),
            new Vector3(0, 3, 0)
        );

        FixedDisplayTransform = new Transform3D(
            Basis.Identity.Scaled(new Vector3(0.5f, 0.5f, 0.5f)),
            new Vector3(0, 0, 0)
        );

        ThirdPersonRightHandTransform = new Transform3D(
            Basis
                .Identity.Rotated(Vector3.Right, Mathf.DegToRad(75))
                .Rotated(Vector3.Up, Mathf.DegToRad(45))
                .Scaled(new Vector3(0.375f, 0.375f, 0.375f)),
            new Vector3(0, 2.5f, 0)
        );

        FirstPersonRightHandTransform = new Transform3D(
            Basis
                .Identity.Rotated(Vector3.Up, Mathf.DegToRad(45))
                .Scaled(new Vector3(0.40f, 0.40f, 0.40f)),
            new Vector3(0, 0, 0)
        );

        FirstPersonLeftHandTransform = new Transform3D(
            Basis
                .Identity.Rotated(Vector3.Up, Mathf.DegToRad(225))
                .Scaled(new Vector3(0.40f, 0.40f, 0.40f)),
            new Vector3(0, 0, 0)
        );
    }
}
