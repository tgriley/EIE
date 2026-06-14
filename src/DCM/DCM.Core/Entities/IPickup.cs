namespace DCM.Core.Entities;

public interface IPickup : IBillboard
{
    bool TryCollect(Player player);
}
