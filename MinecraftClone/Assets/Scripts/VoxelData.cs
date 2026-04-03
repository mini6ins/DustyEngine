using DustyEngine.Engine.Math.Vectors;

namespace MinecraftClone.Assets.Scripts;

public static class VoxelData
{
    public static readonly int ChunkWidth = 5;
    public static readonly int ChunkHeight = 15;

    public static readonly Vector3[] Verts =
    [
        new Vector3(0, 0, 0), // 0
        new Vector3(1, 0, 0), // 1
        new Vector3(1, 1, 0), // 2
        new Vector3(0, 1, 0), // 3
        new Vector3(0, 0, 1), // 4
        new Vector3(1, 0, 1), // 5
        new Vector3(1, 1, 1), // 6
        new Vector3(0, 1, 1), // 7
    ];

    public static readonly Vector3[] FaceChecks =
    [
        new Vector3( 0,  0, -1), // Back
        new Vector3( 0,  0,  1), // Front
        new Vector3( 0,  1,  0), // Top
        new Vector3( 0, -1,  0), // Bottom
        new Vector3(-1,  0,  0), // Left
        new Vector3( 1,  0,  0), // Right
    ];

    public static readonly int[,] VoxelTris = new int[6, 4]
    {
        { 0, 3, 1, 2 }, // Back
        { 5, 6, 4, 7 }, // Front
        { 3, 7, 2, 6 }, // Top
        { 1, 5, 0, 4 }, // Bottom
        { 4, 7, 0, 3 }, // Left
        { 1, 2, 5, 6 }, // Right
    };
}