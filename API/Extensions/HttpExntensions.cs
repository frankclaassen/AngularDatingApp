using System.Text.Json;
using API.Helpers;

namespace API.Extensions;

public static class HttpExntensions
{
  private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

  public static void AddPaginationHeader(this HttpResponse response, PaginationHeader header)
  {
    response.Headers.Append("Pagination", JsonSerializer.Serialize(header, _jsonOptions));
    response.Headers.Append("Access-Control-Expose-Headers", "Pagination");
  }
}
