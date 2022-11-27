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
        public const int MaxLod = 2;

        public struct ChunkData
        {
            public Vector3Int Pos;
            public int Lod;

            public ChunkData(int x, int y, int z, int lod)
            {
                Pos = new Vector3Int(x, y, z);
                Lod = lod;
            }
        }
        
        public struct RenderNode
        {
            
            private readonly RenderNode[] children;
            private bool hasChildren;

            private readonly int x, y, z;
            private readonly int lod;
            private readonly int halfSize;

            private bool active;

            public RenderNode(int x, int y, int z, int size, int lod)
            {
                this.lod = lod;
                this.x = x;
                this.y = y;
                this.z = z;
                
                halfSize = size / 2;
                active = true;
                hasChildren = false;
                children = new RenderNode[8];
            }

            public RenderNode InactiveNode()
            {
                return new RenderNode
                {
                    active = false,
                    hasChildren = false,
                };
            }

            public void Update(int playerX, int playerY, int playerZ, List<ChunkData> addChunkPositions,
                List<ChunkData> removeChunkPositions, RenderNode oldNode, bool firstGen)
            {
                float distance = Distance(playerX, playerY, playerZ, x + halfSize, y + halfSize, z + halfSize);
                
                if (distance < PlayerNearSize && lod != 0)
                {
                    Split(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldNode, firstGen);
                }
                else if (distance < PlayerMidSize && lod > 1)
                {
                    Split(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldNode, firstGen);
                }
                else
                {
                    // No need to split.
                    if (firstGen || oldNode.hasChildren || !oldNode.active)
                    {
                        if (!firstGen && oldNode.active)
                        {
                            // Remove children of old node.
                            oldNode.RemoveBranch(removeChunkPositions);
                        }

                        // Add this node.
                        addChunkPositions.Add(new ChunkData(x, y, z, lod));
                    }
                }
            }

            private void Split(int playerX, int playerY, int playerZ, List<ChunkData> addChunkPositions,
                List<ChunkData> removeChunkPositions, RenderNode oldNode, bool firstGen)
            {
                hasChildren = true;
                
                int newLod = lod - 1;
                int newSize = ChunkSize * GetChunkScale(newLod);
                children[0] = new RenderNode(x, y, z, newSize, newLod);
                children[1] = new RenderNode(x + halfSize, y, z, newSize, newLod);
                children[2] = new RenderNode(x, y + halfSize, z, newSize, newLod);
                children[3] = new RenderNode(x, y, z + halfSize, newSize, newLod);
                children[4] = new RenderNode(x + halfSize, y + halfSize, z, newSize, newLod);
                children[5] = new RenderNode(x, y + halfSize, z + halfSize, newSize, newLod);
                children[6] = new RenderNode(x + halfSize, y, z + halfSize, newSize, newLod);
                children[7] = new RenderNode(x + halfSize, y + halfSize, z + halfSize, newSize, newLod);

                bool oldNodeWasLeaf = !oldNode.hasChildren;

                // If this node is splitting and the old node didn't, then the old node is outdated.
                if (!firstGen && oldNodeWasLeaf && oldNode.active)
                {
                    removeChunkPositions.Add(new ChunkData(x, y, z, lod));
                }
                
                for (int i = 0; i < 8; i++)
                {
                    RenderNode oldChild = oldNodeWasLeaf ? InactiveNode() : oldNode.children[i];
                    children[i].Update(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldChild, firstGen);
                }
            }

            private void RemoveBranch(List<ChunkData> removeChunkPositions)
            {
                if (hasChildren)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        children[i].RemoveBranch(removeChunkPositions);
                    }
                }
                else
                {
                    removeChunkPositions.Add(new ChunkData(x, y, z, lod));
                }
            }
        }

        private readonly RenderNode[][] nodes;
        private RenderNode[] currentNodes;
        private RenderNode[] oldNodes;
        
        private int currentNodesI;
        private readonly int lodSizeChunks, lodHeightChunks, maxLodChunkSize;
        private bool firstGen = true;
        
        public readonly List<ChunkData> AddChunkPositions = new();
        public readonly List<ChunkData> RemoveChunkPositions = new();
        
        public RenderTree(int sizeChunks, int heightChunks)
        {
            int scale = GetChunkScale(MaxLod);
            maxLodChunkSize = ChunkSize * scale;

            lodSizeChunks = sizeChunks / scale;
            lodHeightChunks = heightChunks / scale;

            nodes = new RenderNode[2][];

            for (int n = 0; n < 2; n++)
            {
                nodes[n] = new RenderNode[lodSizeChunks * lodHeightChunks * lodSizeChunks];
            }

            currentNodes = nodes[0];
            oldNodes = nodes[1];
        }

        public void Update(int playerX, int playerY, int playerZ)
        {
            AddChunkPositions.Clear();
            RemoveChunkPositions.Clear();
            
            int i = 0;
            for (int x = 0; x < lodSizeChunks; x++)
            {
                for (int y = 0; y < lodHeightChunks; y++)
                {
                    for (int z = 0; z < lodSizeChunks; z++)
                    {
                        currentNodes[i] = new RenderNode(x * maxLodChunkSize, y * maxLodChunkSize, z * maxLodChunkSize,
                            maxLodChunkSize, MaxLod);
                        i++;
                    }
                }
            }
            
            for (int j = 0; j < currentNodes.Length; j++)
            {
                currentNodes[j].Update(playerX, playerY, playerZ, AddChunkPositions, RemoveChunkPositions, oldNodes[j], firstGen);
            }
            
            SwapNodes();

            firstGen = false;
        }

        private void SwapNodes()
        {
            oldNodes = nodes[currentNodesI];
            currentNodesI = (currentNodesI + 1) % 2;
            currentNodes = nodes[currentNodesI]; 
        }
        
        private static float Distance(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            return MathF.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1) + (z0 - z1) * (z0 - z1));
        }

        private static int GetChunkScale(int lod)
        {
            int size = 1;

            for (int i = 0; i < lod; i++)
            {
                size *= 2;
            }

            return size;
        }
    }
}
