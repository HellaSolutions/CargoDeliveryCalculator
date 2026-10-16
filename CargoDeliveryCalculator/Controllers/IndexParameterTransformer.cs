using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Controllers;

// The {index} route value is bound as a string so that invalid names reach MarketDataService
// and get its "Invalid index" error; this only documents the accepted values.
public class IndexParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var parameter = operation.Parameters?.FirstOrDefault(p => p is { Name: "index", In: ParameterLocation.Path });
        if (parameter is not OpenApiParameter { Schema: OpenApiSchema schema } indexParameter)
            return Task.CompletedTask;
        indexParameter.Description = "Market index (case-insensitive)";
        schema.Enum = [.. Enum.GetNames<Index>().Select(JsonNode (name) => JsonValue.Create(name))];

        return Task.CompletedTask;
    }
}
