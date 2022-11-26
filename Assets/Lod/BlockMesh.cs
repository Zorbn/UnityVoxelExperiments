using UnityEngine;

namespace Lod
{
    public static class BlockMesh
    {
        public static readonly Vector3[][] FaceVertices =
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
    
        public static readonly int[][] FaceIndices =
        {
            new[] { 0, 2, 1, 0, 3, 2 },
            new[] { 0, 1, 2, 0, 2, 3 },
            new[] { 0, 1, 2, 0, 2, 3 },
            new[] { 0, 2, 1, 0, 3, 2 },
            new[] { 0, 2, 1, 0, 3, 2 },
            new[] { 0, 1, 2, 0, 2, 3 },
        };
    
        public static readonly Vector3Int[] FaceDirections =
        {
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1),
        };
    }
}