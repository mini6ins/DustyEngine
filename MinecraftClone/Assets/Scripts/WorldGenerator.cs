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

public class WorldGenerator : MonoBehaviour
{
    public Transform player;
    public Vector3 spawn;
    
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerLastChunkCoord;
    
    private void Start()
    {
        // var chunkPosition = new Vector3(0, 0, 0);
        // var chunk = new GameObject("Chunk:" + chunkPosition.X + "," + chunkPosition.Y + "," + chunkPosition.Z);
        // chunk.AddComponent(new Chunk());
        // Instantiate(chunk, GameObject, chunkPosition, new Quaternion(), new Vector3(1, 1, 1));
        //
        //
        blocktypes = new BlockType[]
        {
            new BlockType { blockName = "Air",   isSolid = false },
            new BlockType { blockName = "Bedrock", isSolid = true },
            new BlockType { blockName = "Stone", isSolid = true },
            new BlockType { blockName = "Grass", isSolid = true },
        };
        player = SceneManager.FindCameras().FirstOrDefault().GameObject.GetComponent<Transform>();
        
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.LocalPosition);

    }
    
    private void Update() {

        if (!GetChunkCoordFromVector3(player.LocalPosition).Equals(playerLastChunkCoord))
            CheckViewDistance();

    }
    
    private void GenerateWorld () {

        for (int x = VoxelData.WorldSizeInChunks / 2 - VoxelData.ViewDistanceInChunks / 2; x < VoxelData.WorldSizeInChunks / 2 + VoxelData.ViewDistanceInChunks / 2; x++) {
            for (int z = VoxelData.WorldSizeInChunks / 2 - VoxelData.ViewDistanceInChunks / 2; z < VoxelData.WorldSizeInChunks / 2 + VoxelData.ViewDistanceInChunks / 2; z++) {

                CreateChunk(new ChunkCoord(x, z));

            }
        }

        spawn = new Vector3(VoxelData.WorldSizeInBlocks / 2, VoxelData.ChunkHeight + 2, VoxelData.WorldSizeInBlocks / 2);
        player.LocalPosition = spawn;

    }

    private void CheckViewDistance () {
        int chunkX = (int)System.Math.Floor(player.LocalPosition.X / VoxelData.ChunkWidth);
        int chunkZ = (int)System.Math.Floor(player.LocalPosition.Z / VoxelData.ChunkWidth);

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkX - VoxelData.ViewDistanceInChunks / 2; x < chunkX + VoxelData.ViewDistanceInChunks / 2; x++) {
            for (int z = chunkZ - VoxelData.ViewDistanceInChunks / 2; z < chunkZ + VoxelData.ViewDistanceInChunks / 2; z++) {

                if (IsChunkInWorld(x, z)) {

                    ChunkCoord thisChunk = new ChunkCoord(x, z);

                    if (chunks[x, z] == null)
                        CreateChunk(thisChunk);
                    else if (!chunks[x, z].isActive) {
                        chunks[x, z].isActive = true;
                        activeChunks.Add(thisChunk);
                    }
                    for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                        if (previouslyActiveChunks[i].x == x && previouslyActiveChunks[i].z == z)
                            previouslyActiveChunks.RemoveAt(i);

                    }

                }
            }
        }

        foreach (ChunkCoord coord in previouslyActiveChunks)
            chunks[coord.x, coord.z].isActive = false;

    }
    
    
    
    
    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = (int)System.Math.Floor(pos.X / VoxelData.ChunkWidth);
        int z = (int)System.Math.Floor(pos.Z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }
    
    bool IsChunkInWorld(int x, int z) {

        if (x > 0 && x < VoxelData.WorldSizeInChunks - 1 && z > 0 && z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;

    }

    private void CreateChunk (ChunkCoord coord) {

        chunks[coord.x, coord.z] = new Chunk(new ChunkCoord(coord.x, coord.z), this);
        activeChunks.Add(new ChunkCoord(coord.x, coord.z));


    }

    public byte GetVoxel (Vector3 pos) {

        if (pos.X < 0 || pos.X > VoxelData.WorldSizeInBlocks - 1 || pos.Y < 0 || pos.Y > VoxelData.ChunkHeight - 1 || pos.Z < 0 || pos.Z > VoxelData.WorldSizeInBlocks - 1)
            return 0;
        if (pos.Y < 1)
            return 1;
        else if (pos.Y == VoxelData.ChunkHeight - 1)
            return 3;
        else
            return 2;

    }
}


public class ChunkCoord(int x, int z)
{
    public int x = x;
    public int z = z;

    public bool Equals(ChunkCoord other) {

        if (other == null)
            return false;
        else if (other.x == x && other.z == z)
            return true;
        else
            return false;

    }

}


[Serializable]
public class BlockType {

    public string blockName;
    public bool isSolid;

    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID (int faceIndex) {

        switch (faceIndex) {

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