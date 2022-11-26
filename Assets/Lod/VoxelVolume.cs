namespace Lod
{
    public interface VoxelVolume
    {
        public void SetVoxel(int x, int y, int z, bool filled);
        public bool GetVoxel(int x, int y, int z);
    }
}