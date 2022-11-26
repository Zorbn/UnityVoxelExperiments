using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject chunk;
    
    // Start is called before the first frame update
    void Start()
    {
        const int size = 96;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    GameObject newChunk = Instantiate(chunk, new Vector3(x * 32, y * 32, z * 32), Quaternion.identity);
                    newChunk.GetComponent<ChunkRunMesher>().chunkPos = new Vector3Int(x * 32, y * 32, z * 32);
                }
            }   
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
