using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Lod
{
    public class WorldRenderer : MonoBehaviour
    {
        public Transform playerTransform;
        
        public ChunkRendererPool chunkPool;
        public GameObject chunkRenderer;
        
        public int worldSizeInChunks = 81;
        public int worldHeightInChunks = 2;
        public int chunkSize = 32;
        
        private readonly ConcurrentBag<QueuedRenderer> queuedRenderers = new();
        private readonly ConcurrentBag<QueuedRenderer> finishedRenderers = new();
        private readonly List<ChunkRendererPool.ChunkRendererData> removeQueue = new();

        private World world;
        private RenderTree renderTree;

        private Vector3Int playerChunkPos;

        private const int MeshingThreadCount = 4;
        private Thread[] meshingThreads;
        
        private struct QueuedRenderer
        {
            public ChunkRendererPool.ChunkRendererData RendererData;
            public RenderTree.ChunkData ChunkData;
        }

        private readonly Dictionary<Vector3Int, ChunkRendererPool.ChunkRendererData> loadedChunks = new();

        public void Start()
        {
            chunkPool.Init(chunkRenderer);
            
            world = new World(worldSizeInChunks, worldHeightInChunks, worldSizeInChunks, chunkSize);
            world.Generate();
            
            renderTree = new RenderTree(worldSizeInChunks, worldHeightInChunks);

            meshingThreads = new Thread[MeshingThreadCount];
            
            for (int i = 0; i < MeshingThreadCount; i++)
            {
                meshingThreads[i] = new Thread(MeshingThread);
                meshingThreads[i].Start();
            }
        }

        private void MeshingThread()
        {
            while (true)
            {
                while (queuedRenderers.TryTake(out QueuedRenderer nextRenderer))
                {
                    if (!nextRenderer.RendererData.ChunkRenderer) continue;

                    nextRenderer.RendererData.ChunkRenderer.GenerateMesh(world, nextRenderer.ChunkData.Pos.x,
                        nextRenderer.ChunkData.Pos.y, nextRenderer.ChunkData.Pos.z, nextRenderer.ChunkData.Lod);
                    finishedRenderers.Add(nextRenderer);
                }
            }
        }

        private void Update()
        {
            SwapLodAroundPlayer();
            ProcessFinishedRenderers();
        }

        private void SwapLodAroundPlayer()
        {
            Vector3Int newPlayerChunkPos = Vector3Int.FloorToInt(playerTransform.position / chunkSize) * chunkSize;

            if (playerChunkPos != newPlayerChunkPos)
            {
                UpdateChunks();
            }
            
            playerChunkPos = newPlayerChunkPos;
        }

        private void ProcessFinishedRenderers()
        {
            while (finishedRenderers.TryTake(out QueuedRenderer nextRenderer))
            {
                nextRenderer.RendererData.ChunkRenderer.UpdateMesh();
                nextRenderer.RendererData.MeshRenderer.enabled = true;
            }

            if (finishedRenderers.IsEmpty && queuedRenderers.IsEmpty)
            {
                for (int i = removeQueue.Count - 1; i >= 0; i--)
                {
                    removeQueue[i].MeshRenderer.enabled = false;
                    chunkPool.DestroyPooled(removeQueue[i]);
                    removeQueue.RemoveAt(i);
                }
            }
        }

        private void UpdateChunks()
        {
            if (!finishedRenderers.IsEmpty || !queuedRenderers.IsEmpty) return;
            
            Vector3Int playerPos = Vector3Int.RoundToInt(playerTransform.position);

            renderTree.Update(playerPos.x, playerPos.y, playerPos.z);

            foreach (RenderTree.ChunkData data in renderTree.RemoveChunkPositions)
            {
                ChunkRendererPool.ChunkRendererData oldChunk = loadedChunks[data.Pos];
                loadedChunks.Remove(data.Pos);
                removeQueue.Add(oldChunk);
            }
            
            foreach (RenderTree.ChunkData data in renderTree.AddChunkPositions)
            {
                ChunkRendererPool.ChunkRendererData newChunk = chunkPool.InstantiatePooled(data.Pos, Quaternion.identity);
                queuedRenderers.Add(new QueuedRenderer
                {
                    ChunkData = data,
                    RendererData = newChunk
                });
                loadedChunks.Add(data.Pos, newChunk);
            }
        }
    }
}