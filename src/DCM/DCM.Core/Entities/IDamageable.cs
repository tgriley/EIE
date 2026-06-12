namespace DCM.Core.Entities;

public interface IDamageable
{
    double PosX { get; }
    double PosY { get; }
    void TakeDamage(int amount);
}