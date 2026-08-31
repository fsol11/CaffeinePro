// -----------------------------------------------------------------------
// <copyright file="LocExtension.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.Windows.Data;
using System.Windows.Markup;

namespace CaffeinePro.Localization;

/// <summary>
/// Puts a translated string into XAML: <c>Text="{loc:Loc Menu_Quit}"</c>.
/// </summary>
/// <remarks>
/// Produces a binding rather than the string itself, so that switching language updates every
/// piece of text already on screen - see <see cref="LocalizationService"/>.
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    /// <summary>
    /// The resource key, i.e. a <c>name</c> from Resources\Strings.resx.
    /// </summary>
    [ConstructorArgument("key")]
    public string Key
    {
        get;
        set;
    } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
        };

        // Handing the service provider back to the binding is what lets the same markup work both
        // on an element (where it becomes a live binding) and inside a Setter (where WPF stores
        // the binding and applies it per instance).
        return binding.ProvideValue(serviceProvider);
    }
}
