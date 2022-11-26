using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum Direction
{
    XPos,
    XNeg,
    YPos,
    YNeg,
    ZPos,
    ZNeg
}

// internal struct XRun
// {
//     private bool inRun;
//     private int start;
//     private Direction direction;
//     
//     public void StartOrContinue(ChunkRunMesher chunkRunMesher, int x, int y, int z)
//     {
//         if (inRun || !chunkRunMesher.Visible(x, y, z, direction)) return;
//         
//         inRun = true;
//         start = x;
//     }
//
//     public void End(ChunkRunMesher chunkRunMesher)
//     {
//         if (runStartYPos != -1)
//         {
//             runLength = runEnd - runStartYPos;
//             chunkRunMesher.GenerateFaceWithLength(Direction.YPos, runLength, runStartYPos, y, z);
//             runStartYPos = -1;
//         }
//     }
// }
//
// internal struct XRunSet
// {
//     private bool inRun;
//     private XRun yPos;
//     private XRun yNeg;
//     private XRun zPos;
//     private XRun zNeg;
//     
//     public void Step(int block, int x, int y, int z)
//     {
//         if (block == 0 && !inRun) return;
//
//         if (inRun)
//         {
//             if (i == ChunkRunMesher.ChunkSize - 1)
//             {
//                 // End run
//                 GenerateFacesXRun(x);
//                 inRun = false;
//                 return;
//             }
//                     
//             if (block == 0)
//             {
//                 // End run
//                 GenerateFacesXRun(x - 1);
//                 inRun = false;
//                 return;
//             }
//     
//         }
//         else
//         {
//             // Start run
//             inRun = true;
//             // runStart = x;
//         }
//         
//         // Continue run
//         if (runStartYPos == -1 && GetBlock(x, y + 1, z) == 0) runStartYPos = x;
//         if (runStartYNeg == -1 && GetBlock(x, y - 1, z) == 0) runStartYNeg = x;
//         if (runStartZPos == -1 && GetBlock(x, y, z + 1) == 0) runStartZPos = x;
//         if (runStartZNeg == -1 && GetBlock(x, y, z - 1) == 0) runStartZNeg = x;
//     }
// }

[RequireComponent(typeof(MeshFilter))]
public class ChunkRunMesher : MonoBehaviour
{
    private static readonly Vector3[][] faceVertices =
    {
        new Vector3[] {
            new(1, 0, 0), new(1, 0, 1),
            new(1, 1, 1), new(1, 1, 0), 
        },
        new Vector3[] {
            new(0, 0, 0), new(0, 0, 1),
            new(0, 1, 1), new(0, 1, 0), 
        },
        new Vector3[] {
            new(0, 1, 0), new(0, 1, 1),
            new(1, 1, 1), new(1, 1, 0), 
        },
        new Vector3[] {
            new(0, 0, 0), new(0, 0, 1),
            new(1, 0, 1), new(1, 0, 0), 
        },
        new Vector3[] {
            new(0, 0, 1), new(0, 1, 1),
            new(1, 1, 1), new(1, 0, 1), 
        },
        new Vector3[] {
            new(0, 0, 0), new(0, 1, 0),
            new(1, 1, 0), new(1, 0, 0), 
        },
    };

    private static readonly int[][] faceIndices =
    {
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 1, 2, 0, 2, 3 },
        new[] { 0, 1, 2, 0, 2, 3 },
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 1, 2, 0, 2, 3 },
    };

    private static readonly Vector3Int[] faceDirections =
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    };
    
    public List<Vector3> vertices;
    public List<int> indices;
    private MeshFilter meshFilter;
    private Mesh mesh;

    public const int ChunkSize = 32;
    private int[] blocks;
    private int filledBlockCount = 0;

    public bool run = false;

    public Vector3Int chunkPos;

    public bool Visible(int x, int y, int z, Direction direction)
    {
        ref Vector3Int v = ref faceDirections[(int)direction];
        return GetBlock(x + v.x, y + v.y, z + v.z) == 0;
    }
    
    public int GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize ||
            y < 0 || y >= ChunkSize ||
            z < 0 || z >= ChunkSize) return 0;

        return blocks[x + y * ChunkSize + z * ChunkSize * ChunkSize];
    }
    
    private void SetBlock(int x, int y, int z, int block)
    {
        if (x < 0 || x >= ChunkSize ||
            y < 0 || y >= ChunkSize ||
            z < 0 || z >= ChunkSize) return;

        int i = x + y * ChunkSize + z * ChunkSize * ChunkSize;

        if (blocks[i] != 0 && block == 0)
        {
            filledBlockCount--;
        }
        else if (blocks[i] == 0 && block != 0)
        {
            filledBlockCount++;
        }
        
        blocks[i] = block;
    }
    
    // Start is called before the first frame update
    private void Start()
    {
        blocks = new int[ChunkSize * ChunkSize * ChunkSize];
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };
        meshFilter.mesh = mesh;
        
        Generate();

        if (run)
        {
            GenerateMeshRuns();
        }
        else
        {
            GenerateMesh();
        }
    }

    private void Generate()
    {
        if (chunkPos.y != 800.0f)
        {
            return;
        }
        
        for (int z = 0; z < ChunkSize; z++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                float height = Mathf.PerlinNoise(x * 0.07f, z * 0.07f) * ChunkSize;
        
                for (int y = 0; y < height; y++)
                {
                    SetBlock(x, y, z, 1);
                }   
            }
        }
    }

    private void GenerateMesh()
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    int block = GetBlock(x, y, z);
        
                    if (block == 0) continue;
        
                    if (GetBlock(x + 1, y, z) == 0) GenerateFace(Direction.XPos, x, y, z);
                    if (GetBlock(x - 1, y, z) == 0) GenerateFace(Direction.XNeg, x, y, z);
                    if (GetBlock(x, y + 1, z) == 0) GenerateFace(Direction.YPos, x, y, z);
                    if (GetBlock(x, y - 1, z) == 0) GenerateFace(Direction.YNeg, x, y, z);
                    if (GetBlock(x, y, z + 1) == 0) GenerateFace(Direction.ZPos, x, y, z);
                    if (GetBlock(x, y, z - 1) == 0) GenerateFace(Direction.ZNeg, x, y, z);
                }
            }
        }
        
        print($"Standard: {vertices.Count} vertices and {indices.Count} indices");

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
    }
    
    // private void GenerateMeshRuns()
    // {
    //     if (filledBlockCount == 0)
    //     {
    //         gameObject.SetActive(false);
    //         return;
    //     }
    //     
    //     gameObject.SetActive(true);
    //     
    //     int runCount = 0;
    //     
    //     for (int z = 0; z < ChunkSize; z++)
    //     {
    //         for (int y = 0; y < ChunkSize; y++)
    //         {
    //             bool inRun = false;
    //             
    //             // // Y runs
    //             // int runStartXPos = -1;
    //             // int runStartXNeg = -1;
    //             
    //             // X runs
    //             int runStartYPos = -1;
    //             int runStartYNeg = -1;
    //             int runStartZPos = -1;
    //             int runStartZNeg = -1;
    //
    //             void GenerateFacesXRun(int runEnd)
    //             {
    //                 // if (GetBlock(runEnd + 1, y, z) == 0) GenerateFace(Direction.XPos, runEnd, y, z);
    //                 // if (GetBlock(runStart - 1, y, z) == 0) GenerateFace(Direction.XNeg, runStart, y, z);
    //                 
    //                 int runLength;
    //
    //                 if (runStartYPos != -1)
    //                 {
    //                     runLength = runEnd - runStartYPos;
    //                     GenerateFaceWithLength(Direction.YPos, runLength, runStartYPos, y, z);
    //                     runStartYPos = -1;
    //                 }
    //
    //                 if (runStartYNeg != -1)
    //                 {
    //                     runLength = runEnd - runStartYNeg;
    //                     GenerateFaceWithLength(Direction.YNeg, runLength, runStartYNeg, y, z);
    //                     runStartYNeg = -1;
    //                 }
    //
    //                 if (runStartZPos != -1)
    //                 {
    //                     runLength = runEnd - runStartZPos;
    //                     GenerateFaceWithLength(Direction.ZPos, runLength, runStartZPos, y, z);
    //                     runStartZPos = -1;
    //                 }
    //
    //                 if (runStartZNeg != -1)
    //                 {
    //                     runLength = runEnd - runStartZNeg;
    //                     GenerateFaceWithLength(Direction.ZNeg, runLength, runStartZNeg, y, z);
    //                     runStartZNeg = -1;
    //                 }
    //
    //             }
    //             
    //             for (int x = 0; x < ChunkSize; x++)
    //             {
    //                 int block = GetBlock(x, y, z);
    //     
    //                 
    //             }
    //         }
    //     }
    //
    //     print($"Run: {vertices.Count} vertices and {indices.Count} indices with {runCount} runs");
    //     
    //     if (vertices.Count == 0)
    //     {
    //         gameObject.SetActive(false);
    //         return;
    //     }
    //     
    //     mesh.SetVertices(vertices);
    //     mesh.SetIndices(indices, MeshTopology.Triangles, 0);
    // }
    
    private void GenerateMeshRuns()
    {
        if (filledBlockCount == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        int runCount = 0;
        
        for (int z = 0; z < ChunkSize; z++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                bool inRun = false;
                int runStart = 0;
                int runStartYPos = -1;
                int runStartYNeg = -1;
                int runStartZPos = -1;
                int runStartZNeg = -1;
    
                void GenerateFaces(int runEnd)
                {
                    if (GetBlock(runEnd + 1, y, z) == 0) GenerateFace(Direction.XPos, runEnd, y, z);
                    if (GetBlock(runStart - 1, y, z) == 0) GenerateFace(Direction.XNeg, runStart, y, z);
                    
                    int runLength;
    
                    if (runStartYPos != -1)
                    {
                        runLength = runEnd - runStartYPos;
                        GenerateFaceWithLength(Direction.YPos, runLength, runStartYPos, y, z);
                        runStartYPos = -1;
                    }
    
                    if (runStartYNeg != -1)
                    {
                        runLength = runEnd - runStartYNeg;
                        GenerateFaceWithLength(Direction.YNeg, runLength, runStartYNeg, y, z);
                        runStartYNeg = -1;
                    }
    
                    if (runStartZPos != -1)
                    {
                        runLength = runEnd - runStartZPos;
                        GenerateFaceWithLength(Direction.ZPos, runLength, runStartZPos, y, z);
                        runStartZPos = -1;
                    }
    
                    if (runStartZNeg != -1)
                    {
                        runLength = runEnd - runStartZNeg;
                        GenerateFaceWithLength(Direction.ZNeg, runLength, runStartZNeg, y, z);
                        runStartZNeg = -1;
                    }
    
                }
                
                for (int x = 0; x < ChunkSize; x++)
                {
                    int block = GetBlock(x, y, z);
        
                    if (block == 0 && !inRun) continue;
    
                    if (x == ChunkSize - 1)
                    {
                        // End run
                        GenerateFaces(x);
                        inRun = false;
                        continue;
                    }
                    
                    if (block == 0 && inRun)
                    {
                        // End run
                        GenerateFaces(x - 1);
                        inRun = false;
                        continue;
                    }
    
                    if (!inRun)
                    {
                        // Start run
                        inRun = true;
                        runStart = x;
                        runCount++;
                    }
                    
                    // Continue run
                    if (runStartYPos == -1 && GetBlock(x, y + 1, z) == 0) runStartYPos = x;
                    if (runStartYNeg == -1 && GetBlock(x, y - 1, z) == 0) runStartYNeg = x;
                    if (runStartZPos == -1 && GetBlock(x, y, z + 1) == 0) runStartZPos = x;
                    if (runStartZNeg == -1 && GetBlock(x, y, z - 1) == 0) runStartZNeg = x;
                }
            }
        }
    
        print($"Run: {vertices.Count} vertices and {indices.Count} indices with {runCount} runs");
        
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
    }
    
    private void GenerateFaceWithLength(Direction dir, int length, int x, int y, int z)
    {
        int baseVertex = vertices.Count;

        int dirI = (int)dir;
        // for (int i = 0; i < 4; i++)
        // {
        //     vertices.Add(faceVertices[dirI][i] + new Vector3(x, y, z));
        // }
        vertices.Add(faceVertices[dirI][0] + new Vector3(x, y, z));
        vertices.Add(faceVertices[dirI][1] + new Vector3(x, y, z));
        vertices.Add(faceVertices[dirI][2] + new Vector3(x + length, y, z));
        vertices.Add(faceVertices[dirI][3] + new Vector3(x + length, y, z));
        
        for (int i = 0; i < 6; i++)
        {
            indices.Add(faceIndices[dirI][i] + baseVertex);
        }
    }

    private void GenerateFace(Direction dir, int x, int y, int z)
    {
        int baseVertex = vertices.Count;
        
        int dirI = (int)dir;
        for (int i = 0; i < 4; i++)
        {
            vertices.Add(faceVertices[dirI][i] + new Vector3(x, y, z));
        }
        
        for (int i = 0; i < 6; i++)
        {
            indices.Add(faceIndices[dirI][i] + baseVertex);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
