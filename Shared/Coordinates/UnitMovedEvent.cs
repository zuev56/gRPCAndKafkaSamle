namespace Shared.Coordinates;

public record struct UnitMovedEvent(int Id, Point Coordinates, DateTimeOffset Date);