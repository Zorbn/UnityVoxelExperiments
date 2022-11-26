using System.Runtime.CompilerServices;

namespace Lod
{
    public class World : VoxelVolume
    {
        private Chunk[] chunks;
        public readonly int WidthChunks, HeightChunks, DepthChunks, Width, Height, Depth, ChunkSize;
        
        public World(int widthChunks, int heightChunks, int depthChunks, int chunkSize)
        {
            WidthChunks = widthChunks;
            HeightChunks = heightChunks;
            DepthChunks = depthChunks;
            ChunkSize = chunkSize;
            
            Width = widthChunks * chunkSize;
            Height = heightChunks * chunkSize;
            Depth = depthChunks * chunkSize;
            
            chunks = new Chunk[widthChunks * heightChunks * depthChunks];
        }

        public void Generate()
        {
            for (int x = 0; x < WidthChunks; x++)
            {
                for (int y = 0; y < HeightChunks; y++)
                {
                    for (int z = 0; z < DepthChunks; z++)
                    {
                        int chunkI = x + y * WidthChunks + z * WidthChunks * HeightChunks;
                        chunks[chunkI] =
                            new Chunk(ChunkSize, x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        chunks[chunkI].Generate();
                    }
                }   
            }
        }
        
        public void SetVoxel(int x, int y, int z, bool filled)
        {
            if (x < 0 || x >= Width ||
                y < 0 || y >= Height ||
                z < 0 || z >= Depth) return;
            
            int chunkX = x / ChunkSize;
            int chunkY = y / ChunkSize;
            int chunkZ = z / ChunkSize;

            int localX = x % ChunkSize;
            int localY = y % ChunkSize;
            int localZ = z % ChunkSize;
            chunks[GetChunkI(chunkX, chunkY, chunkZ)].SetVoxel(localX, localY, localZ, filled);
        }

        public bool GetVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= Width ||
                y < 0 || y >= Height ||
                z < 0 || z >= Depth) return false;
            
            int chunkX = x / ChunkSize;
            int chunkY = y / ChunkSize;
            int chunkZ = z / ChunkSize;
            
            int localX = x % ChunkSize;
            int localY = y % ChunkSize;
            int localZ = z % ChunkSize;

            return chunks[GetChunkI(chunkX, chunkY, chunkZ)].GetVoxel(localX, localY, localZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkI(int chunkX, int chunkY, int chunkZ)
        {
            return chunkX + chunkY * WidthChunks + chunkZ * WidthChunks * HeightChunks;
        }
    }
}