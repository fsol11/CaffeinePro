using System.Text.Json.Serialization;

namespace CaffeinePro.Classes;

[method: JsonConstructor]
public sealed class AwakenessOptions(
    bool allowScreenSaver,
    bool inactiveWhenLocked,
    bool inactiveWhenOnBattery,
    bool inactiveWhenCpuBelowPercentage,
    int cpuBelowPercentage)
    : IEquatable<AwakenessOptions>
{
    public AwakenessOptions() : this(false, false, false, false, 8)
    {
    }

    public bool AllowScreenSaver
    {
        get;
    } = allowScreenSaver;

    public bool InactiveWhenLocked
    {
        get;
    } = inactiveWhenLocked;


    public bool InactiveWhenOnBattery
    {
        get;
    } = inactiveWhenOnBattery;


    public bool InactiveWhenCpuBelowPercentage
    {
        get;
    } = inactiveWhenCpuBelowPercentage;


    public int CpuBelowPercentage
    {
        get;
    } = cpuBelowPercentage;


    [JsonIgnore]
    public bool AnyOptionsSet => AllowScreenSaver 
                                 || InactiveWhenLocked 
                                 || InactiveWhenOnBattery 
                                 || InactiveWhenCpuBelowPercentage;

    public bool Equals(AwakenessOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        return (other.AllowScreenSaver == AllowScreenSaver &&
                other.InactiveWhenLocked == InactiveWhenLocked &&
                other.InactiveWhenOnBattery == InactiveWhenOnBattery &&
                other.InactiveWhenCpuBelowPercentage == InactiveWhenCpuBelowPercentage &&
                other.CpuBelowPercentage == CpuBelowPercentage);

    }
}