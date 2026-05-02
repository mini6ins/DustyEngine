using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using MinecraftClone.Assets.Scripts;
using SceneSystem.EngineObject.GameObject;
using System;
using System.Collections.Generic;
using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using MinecraftClone.Assets.Scripts;
using SceneSystem.EngineObject.GameObject;
using System.Linq;
using SceneSystem.Scene;

public class WorldGenerator : MonoBehaviour
{
    public int seed;

    public Transform? player;
    public Vector3 spawn;

    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    private void Start()
    {
        seed = Guid.NewGuid().GetHashCode();
        
        blocktypes = new BlockType[]
        {
            new BlockType { blockName = "Air",     isSolid = false }, // 0
            new BlockType { blockName = "Bedrock", isSolid = true  }, // 1
            new BlockType { blockName = "Stone",   isSolid = true  }, // 2
            new BlockType { blockName = "Grass",   isSolid = true  }, // 3
            new BlockType { blockName = "Sand",    isSolid = true  }, // 4  
            new BlockType { blockName = "Dirt",    isSolid = true  }, // 5  
        };

        var camera = ComponentQueryService
            .Collect<Camera>(SceneManager.CurrentScene!.GameObjects)
            .FirstOrDefault();

        player = camera?.GameObject?.GetComponent<Transform>();

        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.LocalPosition);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.LocalPosition);


        if (!GetChunkCoordFromVector3(player.LocalPosition).Equals(playerLastChunkCoord))
            CheckViewDistance();
    }

    private void GenerateWorld()
    {
        for (int x = VoxelData.WorldSizeInChunks / 2 - VoxelData.ViewDistanceInChunks / 2;
             x < VoxelData.WorldSizeInChunks / 2 + VoxelData.ViewDistanceInChunks / 2;
             x++)
        {
            for (int z = VoxelData.WorldSizeInChunks / 2 - VoxelData.ViewDistanceInChunks / 2;
                 z < VoxelData.WorldSizeInChunks / 2 + VoxelData.ViewDistanceInChunks / 2;
                 z++)
            {
                CreateNewChunk(x, z);
            }
        }

        spawn = new Vector3(VoxelData.WorldSizeInBlocks / 2, VoxelData.ChunkHeight + 2,
            VoxelData.WorldSizeInBlocks / 2);
        player.LocalPosition = spawn;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = (int)System.Math.Floor(pos.X / VoxelData.ChunkWidth);
        int z = (int)System.Math.Floor(pos.Z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }
    

    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.LocalPosition);

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.X - VoxelData.ViewDistanceInChunks; x < coord.X + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.Z - VoxelData.ViewDistanceInChunks; z < coord.Z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                        CreateNewChunk(x, z);
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                        activeChunks.Add(new ChunkCoord(x, z));
                    }
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.X, c.Z].IsActive = false;
    }

    public byte GetVoxel(Vector3 pos)
    {
        var yPos = (int)System.Math.Floor(pos.Y);

        if (!IsVoxelInWorld(pos))
            return 0;

        if (yPos == 0)
            return 1;
        
        
        float rawNoise = Noise.Get2DPerlin(new Vector2(pos.X, pos.Z), 0, 0.05f);
        int terrainHeight = (int)System.Math.Floor(20 * rawNoise) + 10;
        byte voxelValue = 0;
        
        if (pos.X < 1 && pos.Z < 1)
            Debug.Log($"pos=({pos.X},{pos.Z}) noise={rawNoise:F4} terrainHeight={terrainHeight}");

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        return voxelValue;
    }

    void CreateNewChunk (int x, int z) {

        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
        activeChunks.Add(new ChunkCoord(x, z));

    }
    
    bool IsChunkInWorld (ChunkCoord coord) {

        if (coord.X > 0 && coord.X < VoxelData.WorldSizeInChunks - 1 && coord.Z > 0 && coord.Z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return
                false;

    }
    
    
    bool IsVoxelInWorld (Vector3 pos) {

        if (pos.X >= 0 && pos.X < VoxelData.WorldSizeInBlocks && pos.Y >= 0 && pos.Y < VoxelData.ChunkHeight && pos.Z >= 0 && pos.Z < VoxelData.WorldSizeInBlocks)
            return true;
        else
            return false;

    }
}





[Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}