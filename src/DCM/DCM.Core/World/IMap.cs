namespace DCM.Core.World
{
    public interface IMap
    {
        int  Width  { get; }
        int  Height { get; }
        int  GetTile(int x, int y);
        bool IsWall(int x, int y);
        bool IsExit(int x, int y);
    }
}
