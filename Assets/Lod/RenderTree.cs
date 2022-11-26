using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lod
{
    public class RenderTree
    {
        public const int PlayerNearSize = 128;
        public const int PlayerMidSize = 256;
        public const int ChunkSize = 32;

        public struct ChunkData
        {
            public int X;
            public int Z;
            public int Lod;

            public ChunkData(int x, int z, int lod)
            {
                X = x;
                Z = z;
                Lod = lod;
            }
        }
        
        public struct RenderNode
        {
            public RenderNode[] children;
            public int lod;
            public int x, z;
            public int size;
            public int halfSize;

            public RenderNode(int x, int z, int size, int lod)
            {
                this.lod = lod;
                this.x = x;
                this.z = z;
                this.size = size;
                halfSize = size / 2;
                children = new RenderNode[4];
            }

            public void Update(int playerX, int playerZ, List<ChunkData> chunkPositions)
            {
                float distance = Distance(playerX, 0, playerZ, x + halfSize, 0, z + halfSize);
                
                if (distance < PlayerNearSize && lod != 0)
                {
                    Split(playerX, playerZ, chunkPositions);
                }
                else if (distance < PlayerMidSize && lod > 1)
                {
                    Split(playerX, playerZ, chunkPositions);
                }
                else
                {
                    // No need to split
                    chunkPositions.Add(new ChunkData(x, z, lod));
                }
            }

            private void Split(int playerX, int playerZ, List<ChunkData> chunkPositions)
            {
                int newLod = lod - 1;
                int newSize = GetChunkSize(newLod);
                children[0] = new RenderNode(x, z, newSize, newLod);
                children[1] = new RenderNode(x + halfSize, z, newSize, newLod);
                children[2] = new RenderNode(x, z + halfSize, newSize, newLod);
                children[3] = new RenderNode(x + halfSize, z + halfSize, newSize, newLod);

                for (int i = 0; i < 4; i++)
                {
                    children[i].Update(playerX, playerZ, chunkPositions);
                }
            }
        }

        public RenderNode[] nodes;
        public List<ChunkData> chunkPositions = new();
        
        public RenderTree(int sizeChunks, int playerX, int playerZ)
        {
            int lodSize = sizeChunks /  4;

            nodes = new RenderNode[lodSize * lodSize];

            int i = 0;
            for (int x = 0; x < lodSize; x++)
            {
                for (int z = 0; z < lodSize; z++)
                {
                    int size = GetChunkSize(2);
                    nodes[i] = new RenderNode(x * size, z * size, size, 2);
                    nodes[i].Update(playerX, playerZ, chunkPositions);
                    i++;
                }
            }
        }
        
        private static float Distance(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            return MathF.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1) + (z0 - z1) * (z0 - z1));
        }

        private static int GetChunkSize(int lod)
        {
            int size = ChunkSize;

            for (int i = 0; i < lod; i++)
            {
                size *= 2;
            }

            return size;
        }
    }
}
