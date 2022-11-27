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
            vertices.Clear();
            indices.Clear();
            
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

                        // Calculate which faces of this block will be visible.
                        // Faces of blocks that normally wouldn't be considered visible but are on the edge of the chunk and
                        // visible from at least one other direction are also added to the mesh to hide seams at LOD boundaries.
                        // TODO: Apply the lod seam logic to the Y direction (when visible on X or Z).
                        bool visibleYPos = !world.GetVoxel(x, y + lod, z);
                        bool visibleYNeg = !world.GetVoxel(x, y - lod, z);
                        bool visibleY = visibleYPos || visibleYNeg;
                        bool visibleXPos = !world.GetVoxel(x + lod, y, z);
                        bool visibleXNeg = !world.GetVoxel(x - lod, y, z);
                        bool visibleX = visibleXPos || visibleXNeg;
                        bool visibleZPos = !world.GetVoxel(x, y, z + lod);
                        bool visibleZNeg = !world.GetVoxel(x, y, z - lod);
                        bool visibleZ = visibleZPos || visibleZNeg;

                        bool visibleYOrX = visibleY || visibleX;
                        bool visibleYOrZ = visibleY || visibleZ;
                        
                        if ((visibleYOrZ && i == size - lod) || visibleXPos) GenerateFace(Direction.XPos, i, j, k, lod);
                        if ((visibleYOrZ && i == 0) || visibleXNeg) GenerateFace(Direction.XNeg, i, j, k, lod);
                        if (visibleYPos) GenerateFace(Direction.YPos, i, j, k, lod);
                        if (visibleYNeg) GenerateFace(Direction.YNeg, i, j, k, lod);
                        if ((visibleYOrX && k == size - lod) || visibleZPos) GenerateFace(Direction.ZPos, i, j, k, lod);
                        if ((visibleYOrX && k == 0) || visibleZNeg) GenerateFace(Direction.ZNeg, i, j, k, lod);
                    }
                }
            }
        }

        public void UpdateMesh()
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
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