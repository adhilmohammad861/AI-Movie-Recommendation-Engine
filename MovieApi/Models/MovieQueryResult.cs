namespace MovieApi.Models;

/// <summary>
/// Keyless result type used by the raw SQL cosine-similarity query in MovieController.
/// Column names must exactly match the SQL SELECT aliases (Database.SqlQueryRaw
/// maps by column name, not EF model configuration).
/// </summary>
public class MovieQueryResult
{
    public string Title       { get; set; } = string.Empty;
    public string Genre       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Score       { get; set; }
}
