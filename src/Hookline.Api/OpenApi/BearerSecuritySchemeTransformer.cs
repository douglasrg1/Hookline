using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hookline.Api.OpenApi;

// Tells the API document, and the Scalar page built from it, that every operation expects
// "Authorization: Bearer <key>". Scalar then shows a field for the key and sends it with each request.
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "The organization key, with access to every consumer, or a consumer key, limited to its own consumer.",
        };

        // Applies to every operation in the document. /health, the only public route, is not in it.
        document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SchemeName, document)] = [] }];

        return Task.CompletedTask;
    }
}
