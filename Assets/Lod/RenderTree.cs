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
            public RenderNode[] Children;
            public bool HasChildren;
            
            public int Lod;
            public int X, Y, Z;
            public int Size;
            public int HalfSize;
            
            public bool Active;

            public RenderNode(int x, int y, int z, int size, int lod)
            {
                Lod = lod;
                X = x;
                Y = y;
                Z = z;
                Size = size;
                HalfSize = size / 2;
                Active = true;
                HasChildren = false;
                Children = new RenderNode[8];
            }

            public RenderNode InactiveNode()
            {
                return new RenderNode
                {
                    Active = false,
                    HasChildren = false,
                };
            }

            public void Update(int playerX, int playerY, int playerZ, List<ChunkData> addChunkPositions,
                List<ChunkData> removeChunkPositions, RenderNode oldNode, bool firstGen)
            {
                float distance = Distance(playerX, playerY, playerZ, X + HalfSize, Y + HalfSize, Z + HalfSize);
                
                if (distance < PlayerNearSize && Lod != 0)
                {
                    Split(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldNode, firstGen);
                }
                else if (distance < PlayerMidSize && Lod > 1)
                {
                    Split(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldNode, firstGen);
                }
                else
                {
                    // No need to split.
                    if (firstGen || oldNode.HasChildren || !oldNode.Active)
                    {
                        if (!firstGen && oldNode.Active)
                        {
                            // Remove children of old node.
                            oldNode.RemoveBranch(removeChunkPositions);
                        }

                        // Add this node.
                        addChunkPositions.Add(new ChunkData(X, Y, Z, Lod));
                    }
                }
            }

            private void Split(int playerX, int playerY, int playerZ, List<ChunkData> addChunkPositions,
                List<ChunkData> removeChunkPositions, RenderNode oldNode, bool firstGen)
            {
                HasChildren = true;
                
                int newLod = Lod - 1;
                int newSize = ChunkSize * GetChunkScale(newLod);
                Children[0] = new RenderNode(X, Y, Z, newSize, newLod);
                Children[1] = new RenderNode(X + HalfSize, Y, Z, newSize, newLod);
                Children[2] = new RenderNode(X, Y + HalfSize, Z, newSize, newLod);
                Children[3] = new RenderNode(X, Y, Z + HalfSize, newSize, newLod);
                Children[4] = new RenderNode(X + HalfSize, Y + HalfSize, Z, newSize, newLod);
                Children[5] = new RenderNode(X, Y + HalfSize, Z + HalfSize, newSize, newLod);
                Children[6] = new RenderNode(X + HalfSize, Y, Z + HalfSize, newSize, newLod);
                Children[7] = new RenderNode(X + HalfSize, Y + HalfSize, Z + HalfSize, newSize, newLod);

                bool oldNodeWasLeaf = !oldNode.HasChildren;

                // If this node is splitting and the old node didn't, then the old node is outdated.
                if (!firstGen && oldNodeWasLeaf && oldNode.Active)
                {
                    removeChunkPositions.Add(new ChunkData(X, Y, Z, Lod));
                }
                
                for (int i = 0; i < 8; i++)
                {
                    RenderNode oldChild = oldNodeWasLeaf ? InactiveNode() : oldNode.Children[i];
                    Children[i].Update(playerX, playerY, playerZ, addChunkPositions, removeChunkPositions, oldChild, firstGen);
                }
            }

            private void RemoveBranch(List<ChunkData> removeChunkPositions)
            {
                if (HasChildren)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Children[i].RemoveBranch(removeChunkPositions);
                    }
                }
                else
                {
                    removeChunkPositions.Add(new ChunkData(X, Y, Z, Lod));
                }
            }
        }

        private RenderNode[][] nodes;
        private RenderNode[] currentNodes;
        private RenderNode[] oldNodes;
        private int currentNodesI;
        private int lodSizeChunks, lodHeightChunks, maxLodChunkSize;
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
