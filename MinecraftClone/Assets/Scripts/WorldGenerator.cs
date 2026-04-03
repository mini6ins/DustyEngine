using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using MinecraftClone.Assets.Scripts;
using SceneSystem.EngineObject.GameObject;

public class WorldGenerator : MonoBehaviour
{
    public Transform player;

    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerLastChunkCoord;
    
    private void Start()
    {
        var chunkPosition = new Vector3(0, 0, 0);
        var chunk = new GameObject("Chunk:" + chunkPosition.X + "," + chunkPosition.Y + "," + chunkPosition.Z);
        chunk.AddComponent(new Chunk());
        Instantiate(chunk, GameObject, chunkPosition, new Quaternion(), new Vector3(1, 1, 1));
    }
}