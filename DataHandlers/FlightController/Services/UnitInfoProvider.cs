using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Shared.Coordinates;

namespace FlightController.Services;

public sealed class UnitInfoProvider : UnitInfo.UnitInfoBase
{
    internal static readonly Dictionary<int, Point> UnitIdToLastPoint = new();

    public override Task<UnitInfoResponse> Provide(Empty request, ServerCallContext context)
    {
        var response = new UnitInfoResponse();

        foreach (var unitIdToPoint in UnitIdToLastPoint)
        {
            response.Items.Add(new UnitInfoItem
            {
                Id = unitIdToPoint.Key,
                Name = $"Unit_{unitIdToPoint.Key}",
                X =  unitIdToPoint.Value.X,
                Y = unitIdToPoint.Value.Y
            });
        }

        return Task.FromResult(response);
    }
}