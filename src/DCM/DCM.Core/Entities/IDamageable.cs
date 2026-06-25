namespace DCM.Core.Entities;

public interface IDamageable
{
    double PosX { get; }
    double PosY { get; }
    void TakeDamage(int amount, double sourceX = 0, double sourceY = 0);
}