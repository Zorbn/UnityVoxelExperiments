using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lod
{
    [RequireComponent(typeof(Mesh))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour
    {
        private Mesh mesh;
        private MeshFilter meshFilter;

        private List<Vector3> vertices = new();
        private List<int> indices = new();
        private bool needsUpdate;
        
        private void Awake()
        {
            mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
        }

        public void GenerateMesh(World world, int chunkX, int chunkY, int chunkZ, int lodLevel)
        {
            int lod = 1;

            for (int i = 0; i < lodLevel; i++)
            {
                lod *= 2;
            }

            int size = world.ChunkSize * lod;
            
            for (int k = 0; k < size; k += lod)
            {
                for (int j = 0; j < size; j += lod)
                {
                    for (int i = 0; i < size; i += lod)
                    {
                        int x = chunkX + i;
                        int y = chunkY + j;
                        int z = chunkZ + k;
                        
                        bool block = world.GetVoxel(x, y, z);
        
                        if (!block) continue;
        
                        if (!world.GetVoxel(x + lod, y, z)) GenerateFace(Direction.XPos, i, j, k, lod);
                        if (!world.GetVoxel(x - lod, y, z)) GenerateFace(Direction.XNeg, i, j, k, lod);
                        if (!world.GetVoxel(x, y + lod, z)) GenerateFace(Direction.YPos, i, j, k, lod);
                        if (!world.GetVoxel(x, y - lod, z)) GenerateFace(Direction.YNeg, i, j, k, lod);
                        if (!world.GetVoxel(x, y, z + lod)) GenerateFace(Direction.ZPos, i, j, k, lod);
                        if (!world.GetVoxel(x, y, z - lod)) GenerateFace(Direction.ZNeg, i, j, k, lod);
                    }
                }
            }

            needsUpdate = true;
        }

        private void Update()
        {
            if (needsUpdate)
            {
                mesh.SetVertices(vertices);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                needsUpdate = false;
            }
        }

        private void GenerateFace(Direction dir, int x, int y, int z, int lod)
        {
            int baseVertex = vertices.Count;
        
            int dirI = (int)dir;
            for (int i = 0; i < 4; i++)
            {
                vertices.Add(BlockMesh.FaceVertices[dirI][i] * lod + new Vector3(x, y, z));
            }
        
            for (int i = 0; i < 6; i++)
            {
                indices.Add(BlockMesh.FaceIndices[dirI][i] + baseVertex);
            }
        }
    }
}