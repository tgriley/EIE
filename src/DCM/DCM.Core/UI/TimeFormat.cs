namespace DCM.Core.UI;

public static class TimeFormat
{
    public static string Format(float seconds)
    {
        if (seconds >= float.MaxValue) return "--:--.--";
        var minutes = (int)seconds / 60;
        var wholeSeconds = (int)seconds % 60;
        var tenths = (int)(seconds % 1f * 10f);
        return $"{minutes}:{wholeSeconds:D2}.{tenths}";
    }
}
