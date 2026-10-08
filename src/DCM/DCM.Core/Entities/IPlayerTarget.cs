namespace DCM.Core.Entities;

// What an enemy needs from its prey: where it is and which way it faces (to
// know when it is being looked at), and a way to hurt it.
public interface IPlayerTarget : ICamera
{
    void TakeDamage(int amount, double sourceX, double sourceY);
}
