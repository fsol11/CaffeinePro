// -----------------------------------------------------------------------
// <copyright file="UiRefresher.cs" company="Lotrasoft Inc.">
//     Copyright (c) 2026 Lotrasoft Inc. All rights reserved.
// </copyright>
// <author>Farshid Solimanpour</author>
// -----------------------------------------------------------------------
// This file is part of the Caffeine Pro project.
// The Caffeine Pro project is licensed under MIT License.
// For more details, see the LICENSE file in the project root.
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace CaffeinePro.Localization;

/// <summary>
/// Re-reads every binding in the UI that is currently on screen.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="LocExtension"/> binding refreshes itself when the language changes, because it
/// reads from <see cref="LocalizationService"/> and that announces its indexer. But plenty of text
/// is produced by a converter from a source that has not changed at all - a duration turned into
/// "2h : 30m", an awakeness turned into "Until 5:00 PM". Those bindings have no reason to
/// re-evaluate, so without this the time picker would keep showing the previous language until the
/// user happened to move the slider.
/// </para>
/// <para>
/// Rather than making every one of those bindings carry the language as a second source - easy to
/// forget on the next binding someone writes - the refresh is done in one place, by walking what is
/// on screen and asking each binding to read its source again. It only runs when the user picks a
/// language, so the cost of the walk does not matter.
/// </para>
/// </remarks>
internal static class UiRefresher
{
    /// <summary>
    /// Refreshes every open window plus the tray menu, which is the one piece of UI that is built
    /// once and then kept alive for the lifetime of the app.
    /// </summary>
    public static void RefreshAll()
    {
        var visited = new HashSet<DependencyObject>();

        foreach (Window window in Application.Current.Windows)
        {
            Refresh(window, visited);
        }

        if (App.CurrentApp.TrayIcon?.ContextMenu is { } trayMenu)
        {
            Refresh(trayMenu, visited);
        }
    }

    /// <summary>
    /// Refreshes one subtree, for UI that hangs off a property rather than off a window - a flyout
    /// assigned to a button, say, which <see cref="RefreshAll"/> has no way to reach.
    /// </summary>
    public static void Refresh(DependencyObject root) => Refresh(root, []);

    /// <summary>
    /// Every dependency property a type declares, cached per type.
    /// </summary>
    /// <remarks>
    /// Asking the type is what makes this work. The obvious shortcut - walking an element's local
    /// values - only sees properties set on the instance, and a binding that came from a
    /// DataTemplate, a ControlTemplate or a Style setter is not one of those. That is exactly how
    /// the time picker's item labels are bound, so a local-value walk skips the very text this
    /// class exists to refresh.
    /// </remarks>
    private static DependencyProperty[] DependencyPropertiesOf(Type type)
    {
        if (PropertyCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var properties = TypeDescriptor.GetProperties(type)
            .Cast<PropertyDescriptor>()
            .Select(descriptor => DependencyPropertyDescriptor.FromProperty(descriptor)?.DependencyProperty)
            .OfType<DependencyProperty>()
            .ToArray();

        PropertyCache[type] = properties;
        return properties;
    }

    private static readonly Dictionary<Type, DependencyProperty[]> PropertyCache = [];

    private static void Refresh(DependencyObject target, HashSet<DependencyObject> visited)
    {
        // The same object can be reached through both trees (and a ContextMenu through its owner),
        // so the set is what stops the walk from looping.
        if (!visited.Add(target))
        {
            return;
        }

        foreach (var property in DependencyPropertiesOf(target.GetType()))
        {
            BindingOperations.GetBindingExpressionBase(target, property)?.UpdateTarget();
        }

        // Both trees: the logical one reaches menu items and the Runs inside a TextBlock, the
        // visual one reaches everything a control template generated.
        foreach (var child in LogicalTreeHelper.GetChildren(target).OfType<DependencyObject>())
        {
            Refresh(child, visited);
        }

        // A context menu, a tooltip or a drop-down's flyout is its own popup, hanging off a
        // property rather than sitting in either tree, so neither walk above would ever arrive
        // there. The flyout matters most: it holds the whole time picker, and without this the
        // picker keeps the language - and the reading direction - it was last opened with.
        if (target is FrameworkElement element)
        {
            if (element.ContextMenu is { } contextMenu)
            {
                Refresh(contextMenu, visited);
            }

            if (element.ToolTip is DependencyObject toolTip)
            {
                Refresh(toolTip, visited);
            }
        }

        if (target is DropDownButton { Flyout: DependencyObject flyout })
        {
            Refresh(flyout, visited);
        }

        if (target is not Visual and not System.Windows.Media.Media3D.Visual3D)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(target); i++)
        {
            Refresh(VisualTreeHelper.GetChild(target, i), visited);
        }
    }
}
