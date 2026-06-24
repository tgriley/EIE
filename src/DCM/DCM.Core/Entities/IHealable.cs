namespace DCM.Core.Entities;

public interface IHealable
{
    double PosX { get; }
    double PosY { get; }
    void Heal(int amount);
}
