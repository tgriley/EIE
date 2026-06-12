namespace DCM.Core.Entities;

public interface ICamera
{
    double PosX { get; }
    double PosY { get; }
    double DirX { get; }
    double DirY { get; }
    double PlaneX { get; }
    double PlaneY { get; }
}