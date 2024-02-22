using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Caffeine_Pro.Classes;

public class Globalization
{
    public static Globalization Default { get; } = new();

    public class Language(string name, string culture)
    {
        public string Name { get; set; } = name;
        public string Culture { get; set; } = culture;
    }

    public IEnumerable<Language> Languages { get; } =
    [
        new ("English", "en"),
        new ("Русский", "ru"),
        new ("Español", "es"),
        new ("Français", "fr"),
        new ("Deutsch", "de"),
        new ("Italiano", "it"),
        new ("Português", "pt"),
        new ("Nederlands", "nl"),
        new ("Polski", "pl"),
        new ("Türkçe", "tr"),
        new ("العربية", "ar"),
        new ("日本語", "ja"),
        new ("한국어", "ko"),
        new ("中文", "zh"),
        new ("עברית", "he"),
        new ("Українська", "uk"),
        new ("Ελληνικά", "el"),
        new ("Svenska", "sv"),
        new ("Dansk", "da"),
        new ("Suomi", "fi"),
        new ("Norsk", "no"),
        new ("Čeština", "cs"),
        new ("Slovenčina", "sk"),
        new ("Magyar", "hu"),
        new ("Hrvatski", "hr"),
        new ("Српски", "sr"),
        new ("Български", "bg"),
        new ("Română", "ro"),
        new ("Eesti", "et"),
        new ("Lietuvių", "lt"),
        new ("Latviešu", "lv"),
        new ("Slovenščina", "sl"),
        new ("Македонски", "mk"),
        new ("Shqip", "sq"),
        new ("Беларуская", "be"),
        new ("ქართული", "ka"),
        new ("Azərbaycanca", "az"),
        new ("Հայերեն", "hy"),
    ];

    private readonly ResourceManager _resourceManager = new("CaffeinePro.Resources", typeof(Globalization).Assembly);

    // Initialize the ResourceManager to use the Resources.resx file

    public string? GetString(string name)
    {
        // Get the string that matches the current culture
        return _resourceManager.GetString(name, CultureInfo.CurrentCulture);
    }

    public void SetCulture(string cultureName)
    {
        // Set the current culture
        CultureInfo.CurrentCulture = new CultureInfo(cultureName);
    }

    public List<CultureInfo> GetAvailableCultures()
    {
        var result = new List<CultureInfo>();

        // Get the path of the main assembly
        var mainAssemblyPath = Assembly.GetExecutingAssembly().Location;
        var mainAssemblyDirectory = Path.GetDirectoryName(mainAssemblyPath);

        // Get the name of the main assembly
        var mainAssemblyName = AssemblyName.GetAssemblyName(mainAssemblyPath).Name;

        // Get the culture names of all satellite assemblies
        if (mainAssemblyDirectory != null)
            foreach (var directory in Directory.GetDirectories(mainAssemblyDirectory))
            {
                var cultureName = Path.GetFileName(directory);
                var satelliteAssemblyPath = Path.Combine(directory, mainAssemblyName + ".resources.dll");

                if (File.Exists(satelliteAssemblyPath))
                {
                    try
                    {
                        var culture = new CultureInfo(cultureName);
                        result.Add(culture);
                    }
                    catch (CultureNotFoundException)
                    {
                        // This happens when the directory does not represent a valid culture; ignore it
                    }
                }
            }

        return result;
    }
}

