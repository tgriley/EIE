namespace DCM.Core.Input;

public readonly struct PlayerInput
{
    public readonly bool MoveForward;
    public readonly bool MoveBack;
    public readonly bool StrafeLeft;
    public readonly bool StrafeRight;
    public readonly bool TurnLeft;
    public readonly bool TurnRight;
    public readonly bool Running;
    public readonly bool CameraRaising;
    public readonly int MouseDeltaX;

    public PlayerInput(bool moveForward, bool moveBack, bool strafeLeft, bool strafeRight,
        bool turnLeft, bool turnRight, bool running, int mouseDeltaX, bool cameraRaising = false)
    {
        MoveForward = moveForward;
        MoveBack = moveBack;
        StrafeLeft = strafeLeft;
        StrafeRight = strafeRight;
        TurnLeft = turnLeft;
        TurnRight = turnRight;
        Running = running;
        CameraRaising = cameraRaising;
        MouseDeltaX = mouseDeltaX;
    }
}