using System.Text.Json.Serialization;

namespace Typedown.Core.Models
{
    public record DimNumber
    {
        // NumberUnit is built through its constructor, so it cannot be populated in place and is replaced instead.
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
        public NumberUnit Unit { get; set; }

        public double Value { get; set; }

        public DimNumber()
        {

        }

        public DimNumber(NumberUnit unit, double value = 0)
        {
            Unit = unit;
            Value = value;
        }

        public double GetValue(NumberUnit targetUnit)
        {
            return ((Value - Unit.Shift) / Unit.Scale) * targetUnit.Scale + targetUnit.Shift;
        }
    }
}
