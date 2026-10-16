using System.Text.Json.Serialization;

namespace CargoDeliveryCalculator.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Index
{
    Hh, Ttf, Nbp, Jkm, Brent
}