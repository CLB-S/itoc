using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkFace : Node3D
{
    private Chunk _targetChunk;
    private ChunkFaceData _chunkFaceData;
    public Direction Direction;
    public Vector3 Normal;

    public void Initialize(Chunk chunk, Direction faceDirection)
    {
        _targetChunk = chunk;
        Direction = faceDirection;
        Normal = faceDirection.Norm();
        _chunkFaceData = chunk.Faces[faceDirection];

        foreach (var (blockID, faceRects) in _chunkFaceData.BlockRects)
        {
            var faceMesh = new ChunkFaceMesh();
            faceMesh.Initialize(this, blockID, faceRects);
            AddChild(faceMesh);
        }
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        this.Visible = IsFaceVisible();
    }

    private bool IsFaceVisible()
    {
        var cameraPosition = CameraHelper.Instance.GetCameraPosition();
        var chunkFacePosition = (_targetChunk.ChunkID +
            Direction.AntiOffset()) * Chunk.SIZE;

        return Normal.Dot(cameraPosition - chunkFacePosition) > 0;
    }
}