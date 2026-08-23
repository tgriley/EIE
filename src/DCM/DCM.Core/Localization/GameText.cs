using System.Globalization;
using System.Resources;

namespace DCM.Core.Localization;

public static class GameText
{
    private static readonly ResourceManager Resources =
        new("DCM.Core.Localization.Resources", typeof(GameText).Assembly);

    public static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
