using System;
using UnityEngine;

namespace Lod
{
    public class Chunk : VoxelVolume
    {
        private bool[] voxels;
        private readonly int size, chunkX, chunkY, chunkZ;

        public Chunk(int size, int chunkX, int chunkY, int chunkZ)
        {
            this.size = size;
            this.chunkX = chunkX;
            this.chunkY = chunkY;
            this.chunkZ = chunkZ;
            voxels = new bool[size * size * size];
        }
        
        public void Generate()
        {
            for (int k = 0; k < size; k++)
            {
                for (int i = 0; i < size; i++)
                {
                    int x = i + chunkX;
                    int z = k + chunkZ;
                    float height = (Mathf.PerlinNoise(x * 0.17f, z * 0.17f) + 0.8f * Mathf.PerlinNoise(x * 0.07f, z * 0.07f) * 0.2f) * 128;
                    int maxY = (int)Math.Min(chunkY + size, height);
                    
                    for (int y = chunkY; y < maxY; y++)
                    {
                        int j = y - chunkY;
                        SetVoxel(i, j, k, true);
                    }   
                }
            }
        }
        
        public void SetVoxel(int x, int y, int z, bool filled)
        {
            voxels[VoxelUtils.GetIndex(x, y, z, size)] = filled;
        }

        public bool GetVoxel(int x, int y, int z)
        {
            return voxels[VoxelUtils.GetIndex(x, y, z, size)];
        }
    }
}