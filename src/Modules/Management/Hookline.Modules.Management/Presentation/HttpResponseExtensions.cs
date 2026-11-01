using Microsoft.AspNetCore.Http;

namespace Hookline.Modules.Management.Presentation;

internal static class HttpResponseExtensions
{
    // For responses that carry a key or a secret: no cache, shared or private, may keep a copy.
    public static void DoNotCache(this HttpResponse response) => response.Headers.CacheControl = "no-store";
}
