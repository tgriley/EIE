namespace DCM.Core.Rendering;

// Everything the renderer needs to draw the held camera: how far it is raised
// (0..1, smoothed), the walk-bob phase, and the white flash intensity (0..1).
public readonly record struct WeaponPose(float Raise, float BobPhase, float Flash);
