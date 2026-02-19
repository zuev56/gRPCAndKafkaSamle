using Microsoft.AspNetCore.Mvc;
using Producer.Shared;
using Producer.Shared.Models;

namespace Producer.WebApi;

public static class PlaceOrderEndpoint
{
    public static IEndpointConventionBuilder MapPostEndpoint(this IEndpointRouteBuilder builder, string pattern = "")
        => builder.MapPost(pattern, PlaceOrderAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> PlaceOrderAsync(
        [FromBody] PlaceOrderRequest placeOrderRequest,
        [FromServices] OrderService orderService,
        CancellationToken cancellationToken)
    {
        if (placeOrderRequest == null!)
            return Results.BadRequest("Request is null");

        await orderService.PlaceOrderAsync(placeOrderRequest, cancellationToken);

        return Results.Ok();
    }
}