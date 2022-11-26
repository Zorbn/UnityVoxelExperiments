using System.Runtime.CompilerServices;

namespace Lod
{
    public static class VoxelUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(int x, int y, int z, int size)
        {
            return x + y * size + z * size * size;
        }
    }
}