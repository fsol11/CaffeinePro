using System.Text.Json.Serialization;

namespace CaffeinePro.Classes;

[method: JsonConstructor]
public sealed class AwakenessOptions(
    bool inactiveWhenOnBattery,
    bool inactiveWhenCpuBelowPercentage,
    int cpuBelowPercentage)
    : IEquatable<AwakenessOptions>
{
    public AwakenessOptions() : this(false, false, 8)
    {
    }

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
    public bool AnyOptionsSet => InactiveWhenOnBattery
                                 || InactiveWhenCpuBelowPercentage;

    public bool Equals(AwakenessOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        return (other.InactiveWhenOnBattery == InactiveWhenOnBattery &&
                other.InactiveWhenCpuBelowPercentage == InactiveWhenCpuBelowPercentage &&
                other.CpuBelowPercentage == CpuBelowPercentage);

    }
}