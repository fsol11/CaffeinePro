using System.ComponentModel;

namespace TimePicker;

public enum HalfDaySign
{
    /// <summary>
    /// AM, between midnight and noon (0:00 - 12:00)
    /// </summary>
    [Description("AM")]
    // ReSharper disable once InconsistentNaming
    AM,

    /// <summary>
    /// PM, between noon and midnight (12:00 - 0:00)
    /// </summary>
    [Description("PM")]
    // ReSharper disable once InconsistentNaming
    PM
}
