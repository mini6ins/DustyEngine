using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using System.Collections.Generic;
using SceneSystem.EngineObject.GameObject;

namespace MinecraftClone.Assets.Scripts;

public class Chunk : MonoBehaviour
{
    MeshRenderer meshRenderer;

    public ChunkCoord coord;
    GameObject chunkObject;

    private int _vertexIndex = 0;
    private readonly List<float> _vertices = new();
    private readonly List<uint> _indices = new();

    private readonly byte[,,] _voxelMap = new byte[
        VoxelData.ChunkWidth,
        VoxelData.ChunkHeight,
        VoxelData.ChunkWidth];


    private WorldGenerator _world;

    public Chunk(ChunkCoord _coord, WorldGenerator world)
    {
        coord = _coord;
        chunkObject = new GameObject();
        var transform = chunkObject.GetComponent<Transform>();
        if (transform == null)
        {
            transform = new Transform();
            chunkObject.AddComponent(transform);
        }
        transform.LocalPosition = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);

        chunkObject.AddComponent(new MeshRenderer(null));
        meshRenderer = chunkObject.GetComponent<MeshRenderer>();

        _world = world;

        chunkObject.Parent = _world.GameObject;
        // meshRenderer.material = _world.material;

        chunkObject.Name = coord.x + ", " + coord.z;

        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }

    public bool isActive
    {
        get { return chunkObject.ActiveSelf; }
        set { chunkObject.SetActive(value); }
    }

    Vector3 position
    {
        get { return chunkObject.GetComponent<Transform>().LocalPosition; }
    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 ||
            z > VoxelData.ChunkWidth - 1)
            return false;
        else return true;
    }

    // private void Start()
    // {
    //     PopulateVoxelMap();
    //     CreateMeshData();
    //     CreateMesh();
    // }

    public void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    _voxelMap[x, y, z] = _world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }
    }

    public void CreateMeshData()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
    }


    public byte GetVoxelFromMap(Vector3 pos)
    {
        pos -= position;
        return _voxelMap[(int)pos.X, (int)pos.Y, (int)pos.Z];
    }

    bool CheckVoxel(Vector3 pos)
    {
        int x = (int)System.Math.Floor(pos.X);
        int y = (int)System.Math.Floor(pos.Y);
        int z = (int)System.Math.Floor(pos.Z);

        return !IsVoxelInChunk(x, y, z)
            ? _world.blocktypes[_world.GetVoxel(pos + position)].isSolid
            : _world.blocktypes[_voxelMap[x, y, z]].isSolid;
    }


    private void AddVoxelDataToChunk(Vector3 pos)
    {
        for (int p = 0; p < 6; p++)
        {
            if (CheckVoxel(pos + VoxelData.faceChecks[p]))
                continue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 vert = pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, i]];
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




    public void CreateMesh()
    {
        Mesh mesh = new Mesh(_vertices.ToArray(), _indices.ToArray());
        // mesh.uv = uvs.ToArray ();

        // meshFilter.mesh = mesh;
        meshRenderer.SetMesh(mesh);
    }
}