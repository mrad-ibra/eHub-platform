using System.Globalization;
using System.Resources;

namespace eHub.Domain.Resources;

public static class MessageResources
{
    private static readonly ResourceManager Manager = new(
        "eHub.Domain.Resources.Messages",
        typeof(MessageResources).Assembly);

    public static string Get(string key, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var culture = CultureInfo.CurrentUICulture;
        var template = Manager.GetString(key, culture)
            ?? Manager.GetString(key, CultureInfo.InvariantCulture)
            ?? key;

        return args.Length > 0
            ? string.Format(culture, template, args)
            : template;
    }
}
