using System;
using UnityEngine;

namespace Lod
{
    // TODO: Make lod chunks have the same number of voxels per chunk but at a larger scale so that less chunks are needed
    public class WorldRenderer : MonoBehaviour
    {
        public World world;
        public GameObject ChunkRenderer;
        public int worldSizeInChunks = 81;
        public int worldHeightInChunks = 2;
        public int chunkSize = 32;
        public int playerChunkX = 4;
        public int playerChunkY = 0;
        public int playerChunkZ = 4;
        public int maxLod = 4;
        public float lodFalloff = 0.4f;

        private RenderTree renderTree;
        
        public void Start()
        {
            world = new World(worldSizeInChunks, worldHeightInChunks, worldSizeInChunks, chunkSize);
            world.Generate();

            renderTree = new RenderTree(worldSizeInChunks, playerChunkX * chunkSize, playerChunkZ * chunkSize);
            foreach (RenderTree.ChunkData data in renderTree.chunkPositions)
            {
                GameObject chunkRenderer = Instantiate(ChunkRenderer, new Vector3(data.X, 0, data.Z), Quaternion.identity);
                chunkRenderer.GetComponent<ChunkRenderer>().GenerateMesh(world, data.X, 0, data.Z, data.Lod);
            }
            
            // for (int x = 0; x < world.WidthChunks; x++)
            // {
            //     for (int y = 0; y < world.HeightChunks; y++)
            //     {
            //         for (int z = 0; z < world.DepthChunks; z++)
            //         {
            //             float dist = Distance(x, y, z, playerChunkX, playerChunkY, playerChunkZ);
            //
            //             int lod = (int)MathF.Min(dist * lodFalloff, maxLod);
            //
            //             GameObject chunkRenderer = Instantiate(ChunkRenderer, new Vector3(x * chunkSize, y * chunkSize, z * chunkSize), Quaternion.identity);
            //             chunkRenderer.GetComponent<ChunkRenderer>().GenerateMesh(world, x * chunkSize, y * chunkSize, z * chunkSize, lod);
            //         }
            //     }   
            // }
        }
    }
}