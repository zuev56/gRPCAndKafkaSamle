using System.Text.Json.Serialization;

namespace Shared.Coordinates;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Point))]
[JsonSerializable(typeof(UnitMovedEvent))]
public partial class CoordinatesJsonSerializerContext : JsonSerializerContext;