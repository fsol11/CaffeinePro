using System.IO;
using System.Reflection;

namespace Caffeine_Pro;

public class Routines
{
    public static string GetResourceTextFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Resource not found: " + resourceName);
    }
}