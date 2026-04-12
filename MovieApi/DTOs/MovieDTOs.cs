namespace MovieApi.DTOs;

public record RecommendRequest(string Mood);
public record MovieResult(string Title, string Genre, string Description, double Score);
public record SeedResponse(int SeededCount);
