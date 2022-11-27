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
        public GameObject chunkRenderer;
        public int worldSizeInChunks = 81;
        public int worldHeightInChunks = 2;
        public int chunkSize = 32;
        private Stopwatch sw;
        private readonly ConcurrentBag<QueuedRenderer> queuedRenders = new();
        private readonly ConcurrentBag<QueuedRenderer> finishedRenders = new();
        private readonly List<OldChunk> removeQueue = new();

        private World world;
        private RenderTree renderTree;

        private Vector3Int playerChunkPos;

        private const int MeshingThreadCount = 4;
        private Thread[] meshingThreads;
        
        // TODO: Use pool for gameobjects instead of instantiate delete

        private struct QueuedRenderer
        {
            public ChunkRenderer Renderer;
            public RenderTree.ChunkData Data;
        }
        
        private struct OldChunk
        {
            public GameObject GameObject;
            public RenderTree.ChunkData Data;
        }
        
        private readonly Dictionary<Vector3Int, GameObject> loadedChunks = new();

        public void Start()
        {
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
                while (queuedRenders.TryTake(out QueuedRenderer nextRenderer))
                {
                    if (!nextRenderer.Renderer) continue;
                    
                    nextRenderer.Renderer.GenerateMesh(world, nextRenderer.Data.Pos.x, nextRenderer.Data.Pos.y, nextRenderer.Data.Pos.z, nextRenderer.Data.Lod);
                    finishedRenders.Add(nextRenderer);
                }
            }
        }

        private void Update()
        {
            Vector3Int newPlayerChunkPos = Vector3Int.FloorToInt(playerTransform.position / chunkSize) * chunkSize;

            if (playerChunkPos != newPlayerChunkPos)
            {
                UpdateChunks();
            }
            
            playerChunkPos = newPlayerChunkPos;

            while (finishedRenders.TryTake(out QueuedRenderer nextRenderer))
            {
                nextRenderer.Renderer.UpdateMesh();
            }

            if (finishedRenders.IsEmpty && queuedRenders.IsEmpty)
            {
                for (int i = removeQueue.Count - 1; i >= 0; i--)
                {
                    Destroy(removeQueue[i].GameObject);
                    removeQueue.RemoveAt(i);
                }
            }
        }

        private void UpdateChunks()
        {
            if (!finishedRenders.IsEmpty || !queuedRenders.IsEmpty) return;
            
            Vector3Int playerPos = Vector3Int.RoundToInt(playerTransform.position);

            renderTree.Update(playerPos.x, playerPos.y, playerPos.z);

            foreach (RenderTree.ChunkData data in renderTree.RemoveChunkPositions)
            {
                GameObject oldChunk = loadedChunks[data.Pos];
                loadedChunks.Remove(data.Pos);
                
                removeQueue.Add(new OldChunk
                {
                    Data = data,
                    GameObject = oldChunk
                });
            }
            
            foreach (RenderTree.ChunkData data in renderTree.AddChunkPositions)
            {
                GameObject newChunk = Instantiate(chunkRenderer, data.Pos, Quaternion.identity);
                queuedRenders.Add(new QueuedRenderer
                {
                    Data = data,
                    Renderer = newChunk.GetComponent<ChunkRenderer>()
                });
                loadedChunks.Add(data.Pos, newChunk);
            }
        }
    }
}