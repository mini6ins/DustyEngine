using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using System.Collections.Generic;

namespace MinecraftClone.Assets.Scripts;

public class Chunk : MonoBehaviour
{
    private int _vertexIndex = 0;
    private readonly List<float> _vertices = new();
    private readonly List<uint> _indices = new();

    private readonly bool[,,] _voxelMap = new bool[
        VoxelData.ChunkWidth,
        VoxelData.ChunkHeight,
        VoxelData.ChunkWidth];

    private void Start()
    {
        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }

    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
            _voxelMap[x, y, z] = true;
    }

    private void CreateMeshData()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
            AddVoxelDataToChunk(new Vector3(x, y, z));
    }

    private void AddVoxelDataToChunk(Vector3 pos)
    {
        for (int p = 0; p < 6; p++)
        {
            if (CheckVoxel(pos + VoxelData.FaceChecks[p]))
                continue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 vert = pos + VoxelData.Verts[VoxelData.VoxelTris[p, i]];
                _vertices.Add(vert.X);
                _vertices.Add(vert.Y);
                _vertices.Add(vert.Z);
                _vertices.Add(1f);
                _vertices.Add(1f);
                _vertices.Add(1f);
                _vertices.Add(1f);
            }

            _indices.Add((uint)(_vertexIndex + 0));
            _indices.Add((uint)(_vertexIndex + 1));
            _indices.Add((uint)(_vertexIndex + 2));
            _indices.Add((uint)(_vertexIndex + 2));
            _indices.Add((uint)(_vertexIndex + 1));
            _indices.Add((uint)(_vertexIndex + 3));
            _vertexIndex += 4;
        }
    }

    private bool CheckVoxel(Vector3 pos)
    {
        int x = (int)pos.X;
        int y = (int)pos.Y;
        int z = (int)pos.Z;

        if (x < 0 || x >= VoxelData.ChunkWidth ||
            y < 0 || y >= VoxelData.ChunkHeight ||
            z < 0 || z >= VoxelData.ChunkWidth)
            return false;

        return _voxelMap[x, y, z];
    }

    private void CreateMesh()
    {
        var mesh = new Mesh(_vertices.ToArray(), _indices.ToArray());
        GameObject.AddComponent(new MeshRenderer(mesh));
    }
}