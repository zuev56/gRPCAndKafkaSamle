using Shared.Coordinates;

namespace RocketCoordGenerator;

internal record struct Rocket
{
    public required int Id { get; init; }
    public Point Coordinates { get; set; }
}