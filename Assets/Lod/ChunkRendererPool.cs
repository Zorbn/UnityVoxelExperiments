using System.Collections.Generic;
using UnityEngine;

namespace Lod
{
    public class ChunkRendererPool : MonoBehaviour
    {
        public struct ChunkRendererData
        {
            public GameObject GameObject;
            public ChunkRenderer ChunkRenderer;
            public MeshRenderer MeshRenderer;

            public ChunkRendererData(GameObject gameObject)
            {
                GameObject = gameObject;
                ChunkRenderer = gameObject.GetComponent<ChunkRenderer>();
                MeshRenderer = gameObject.GetComponent<MeshRenderer>();
            }
        }
        
        private GameObject pooledObject;
        private Queue<ChunkRendererData> availableObjects;

        public void Init(GameObject chunkRendererObject)
        {
            pooledObject = chunkRendererObject;
            availableObjects = new Queue<ChunkRendererData>();
        }
        
        public ChunkRendererData InstantiatePooled(Vector3 position, Quaternion rotation)
        {
            ChunkRendererData newData;
            
            if (availableObjects.Count < 1)
            {
                newData = new ChunkRendererData(Instantiate(pooledObject, position, rotation));
            }
            else
            {
                newData = availableObjects.Dequeue();
                newData.GameObject.SetActive(true);
                newData.GameObject.transform.position = position;
                newData.GameObject.transform.rotation = rotation;
            }
            
            return newData;
        }

        public void DestroyPooled(ChunkRendererData objectToDestroy)
        {
            objectToDestroy.GameObject.SetActive(false);
            availableObjects.Enqueue(objectToDestroy);
        }
    }
}