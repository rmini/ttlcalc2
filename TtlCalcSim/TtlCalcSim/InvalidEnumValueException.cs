using System;

namespace TtlCalcSim
{
    public class InvalidEnumValueException(Type enumType, object? value)
        : Exception($"Invalid value {value} for enum {enumType.Name}.");
}
