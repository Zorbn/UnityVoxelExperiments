using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        private ConcurrentBag<QueuedRenderer> queuedRenders = new();
        private ConcurrentBag<QueuedRenderer> finishedRenders = new();

        private World world;
        private RenderTree renderTree;

        private Vector3Int playerChunkPos;

        private const int MeshingThreadCount = 4;
        private Thread[] meshingThreads;

        private struct QueuedRenderer
        {
            public ChunkRenderer Renderer;
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
            
            // UpdateChunks();
        }

        private void MeshingThread()
        {
            while (true)
            {
                while (queuedRenders.TryTake(out QueuedRenderer next))
                {
                    if (next.Renderer == null) continue;
                    
                    next.Renderer.GenerateMesh(world, next.Data.Pos.x, next.Data.Pos.y, next.Data.Pos.z, next.Data.Lod);
                    finishedRenders.Add(next);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                UpdateChunks();
            }

            Vector3Int newPlayerChunkPos = Vector3Int.FloorToInt(playerTransform.position / chunkSize) * chunkSize;

            if (playerChunkPos != newPlayerChunkPos)
            {
                UpdateChunks();
            }
            
            playerChunkPos = newPlayerChunkPos;

            if (finishedRenders.TryTake(out QueuedRenderer next))
            {
                next.Renderer.UpdateMesh();
            }
        }

        private void UpdateChunks()
        {
            Vector3Int playerPos = Vector3Int.RoundToInt(playerTransform.position);

            sw = new();
            sw.Start();
            renderTree.Update(playerPos.x, playerPos.y, playerPos.z);
            Debug.Log($"{sw.ElapsedMilliseconds} -> {renderTree.AddChunkPositions.Count}, {renderTree.RemoveChunkPositions.Count}");
            sw.Reset();

            foreach (RenderTree.ChunkData data in renderTree.RemoveChunkPositions)
            {
                GameObject oldChunk = loadedChunks[data.Pos];
                loadedChunks.Remove(data.Pos);
                Destroy(oldChunk);
            }
            
            sw.Start();
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
            Debug.Log($"{sw.ElapsedMilliseconds}");
            sw.Reset();
        }
    }
}