using System.Windows.Controls;

namespace CaffeinePro.Controls;

/// <summary>
/// A TextBox that only allows numeric input
/// </summary>
public class NumericTextBox : TextBox
{
    public bool SelectAllOnFocus { get; set; } = true;

    public int Number
    {
        get
        {
            _ = int.TryParse(Text, out var number);
            return number;
        }
    }

    protected override void OnPreviewTextInput(System.Windows.Input.TextCompositionEventArgs e)
    {
        var ctrl = (e.OriginalSource as TextBox)!;
        if (ctrl.Text.Length - ctrl.SelectionLength + e.Text.Length > MaxLength
            || !decimal.TryParse(e.Text, out _))
        {
            e.Handled = true;
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Space)
        {
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnGotFocus(System.Windows.RoutedEventArgs e)
    {
        if (SelectAllOnFocus)
        {
            SelectAll();
        }

        base.OnGotFocus(e);
    }
}
