using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;
using Npgsql;
using OpenAI;
using OpenAI.Embeddings;
using Pgvector;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class MovieController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly OpenAIClient _openAI;

    public MovieController(AppDbContext db, OpenAIClient openAI)
    {
        _db = db;
        _openAI = openAI;
    }

    // ── 30 hard-coded movies ──────────────────────────────────────────────────
    private static readonly (string Title, string Genre, string Description)[] SeedMovies =
    [
        (
            "The Dark Knight",
            "Action / Thriller",
            "A masked vigilante fights a chaotic criminal mastermind in a corrupt city. Batman must choose between his moral code and the greater good."
        ),
        (
            "The Grand Budapest Hotel",
            "Comedy",
            "A legendary concierge and his lobby boy get entangled in a murder mystery across a fictional European republic. Whimsical and fast-paced with a sharp wit."
        ),
        (
            "Interstellar",
            "Sci-Fi",
            "A former NASA pilot travels through a wormhole to find a new home for humanity. Time bends and emotional bonds are tested across galaxies."
        ),
        (
            "The Notebook",
            "Romance",
            "Two young lovers from different social worlds fall apart and reunite over decades. A story of enduring love told through memory and letters."
        ),
        (
            "Get Out",
            "Horror / Thriller",
            "A Black man visits his white girlfriend's family estate and discovers a terrifying secret. Social commentary wrapped in pure psychological dread."
        ),
        (
            "Spirited Away",
            "Animation / Fantasy",
            "A young girl gets trapped in a spirit world and must work to free herself and her parents. A magical journey about courage and identity."
        ),
        (
            "The Shawshank Redemption",
            "Drama",
            "A man wrongly convicted of murder befriends a fellow prisoner and plans a slow escape. A deeply human story about hope and resilience behind bars."
        ),
        (
            "Knives Out",
            "Mystery / Comedy",
            "A detective investigates the death of a wealthy crime novelist surrounded by scheming family members. A clever whodunit with sharp humor."
        ),
        (
            "Parasite",
            "Thriller / Drama",
            "A poor family infiltrates the household of a wealthy family through deception. A dark twist-filled story about class inequality."
        ),
        (
            "La La Land",
            "Musical / Romance",
            "Two dreamers fall in love in Los Angeles while chasing their artistic ambitions. A bittersweet story about sacrifice and what could have been."
        ),
        (
            "Mad Max: Fury Road",
            "Action",
            "A lone warrior and a rebel leader race across a post-apocalyptic wasteland against a tyrant. Non-stop adrenaline with stunning practical effects."
        ),
        (
            "Her",
            "Sci-Fi / Romance",
            "A lonely writer falls in love with an AI operating system in near-future Los Angeles. A quiet meditation on connection, loneliness, and what makes us human."
        ),
        (
            "Coco",
            "Animation / Family",
            "A young boy accidentally enters the Land of the Dead and must find his great-great-grandfather before dawn. A vibrant celebration of family and memory."
        ),
        (
            "Hereditary",
            "Horror",
            "A family unravels after the death of their secretive grandmother, uncovering a dark supernatural legacy. Slow dread builds into pure terror."
        ),
        (
            "The Social Network",
            "Drama / Biopic",
            "The founding of Facebook told through betrayal, lawsuits, and obsession. A razor-sharp portrait of ambition and friendship destroyed by success."
        ),
        (
            "Whiplash",
            "Drama",
            "An ambitious young drummer pushes himself to the edge under a brutal music teacher. Tension mounts with every rehearsal until the explosive finale."
        ),
        (
            "Jurassic Park",
            "Adventure / Sci-Fi",
            "A theme park built around cloned dinosaurs collapses into chaos when the animals escape. Wonder and terror in equal measure."
        ),
        (
            "Eternal Sunshine of the Spotless Mind",
            "Romance / Sci-Fi",
            "A couple undergoes a procedure to erase each other from memory after a painful breakup. Beautifully strange and emotionally devastating."
        ),
        (
            "The Princess Bride",
            "Fantasy / Comedy",
            "A farmhand embarks on a swashbuckling adventure to rescue the woman he loves from a corrupt prince. Funny, romantic, and endlessly quotable."
        ),
        (
            "Arrival",
            "Sci-Fi",
            "A linguist is recruited to communicate with alien spacecraft that have landed worldwide. A cerebral story where language and time intersect in unexpected ways."
        ),
        (
            "Titanic",
            "Romance / Drama",
            "Two strangers from different classes fall in love aboard the ill-fated ocean liner. Epic romance against a backdrop of historical tragedy."
        ),
        (
            "A Quiet Place",
            "Horror / Thriller",
            "A family survives in a post-apocalyptic world inhabited by creatures that hunt by sound. Tension built entirely through silence."
        ),
        (
            "The Lion King",
            "Animation / Drama",
            "A young lion prince flees his kingdom after his father is murdered by his uncle. A story of guilt, identity, and reclaiming your destiny."
        ),
        (
            "Pulp Fiction",
            "Crime / Drama",
            "Interconnected stories of hitmen, a boxer, and a gangster's wife unfold in nonlinear fashion. Darkly funny and relentlessly cool."
        ),
        (
            "Good Will Hunting",
            "Drama",
            "A janitor with a genius-level intellect is discovered and must choose between opportunity and the comfort of his neighborhood. A quiet story about potential and healing."
        ),
        (
            "The Truman Show",
            "Drama / Sci-Fi",
            "A man slowly discovers his entire life has been a television show watched by millions. A sharp satire on surveillance, reality, and free will."
        ),
        (
            "Inside Out",
            "Animation",
            "The emotions inside a young girl's head struggle to help her cope after her family moves cities. A surprisingly deep exploration of sadness and growing up."
        ),
        (
            "No Country for Old Men",
            "Thriller",
            "A hunter finds drug money in the desert and is relentlessly pursued by a remorseless killer. Bleak, tense, and philosophically haunting."
        ),
        (
            "The Martian",
            "Sci-Fi / Comedy",
            "An astronaut is stranded alone on Mars and must science his way to survival. Optimistic, funny, and genuinely thrilling problem-solving."
        ),
        (
            "Marriage Story",
            "Drama / Romance",
            "A couple navigates a painful divorce while trying to remain good parents and people. Intimate and devastating in equal parts."
        )
    ];

    // ── Helper: call OpenAI embeddings ────────────────────────────────────────
    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        EmbeddingClient client = _openAI.GetEmbeddingClient("text-embedding-3-small");
        OpenAIEmbedding embedding = await client.GenerateEmbeddingAsync(text);
        return embedding.ToFloats().ToArray();
    }

    // ── Helper: float[] → PostgreSQL vector literal '[0.1,0.2,...]' ──────────
    private static string ToVectorString(float[] floats) =>
        "[" + string.Join(",", floats.Select(f => f.ToString("G", CultureInfo.InvariantCulture))) + "]";

    // ── POST /api/seed ────────────────────────────────────────────────────────
    [HttpPost("seed")]
    public async Task<ActionResult<SeedResponse>> Seed()
    {
        int count = 0;

        foreach (var (title, genre, description) in SeedMovies)
        {
            // Idempotent: skip if already seeded
            if (await _db.Movies.AnyAsync(m => m.Title == title))
                continue;

            float[] floats = await GetEmbeddingAsync(description);

            _db.Movies.Add(new Movie
            {
                Title       = title,
                Genre       = genre,
                Description = description,
                Embedding   = new Vector(floats)
            });

            await _db.SaveChangesAsync();
            count++;
        }

        return Ok(new SeedResponse(count));
    }

    // ── POST /api/recommend ───────────────────────────────────────────────────
    [HttpPost("recommend")]
    public async Task<ActionResult<IEnumerable<MovieResult>>> Recommend([FromBody] RecommendRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Mood))
            return BadRequest("Mood cannot be empty.");

        float[] floats   = await GetEmbeddingAsync(req.Mood);
        string vectorStr = ToVectorString(floats);
        var    queryVec  = new NpgsqlParameter("queryVec", vectorStr);

        var rows = await _db.Database
            .SqlQueryRaw<MovieQueryResult>(
                """
                SELECT title, genre, description,
                  CAST(1 - (embedding <=> @queryVec::vector) AS FLOAT8) AS score
                FROM movies
                ORDER BY embedding <=> @queryVec::vector
                LIMIT 3
                """,
                queryVec)
            .ToListAsync();

        return Ok(rows.Select(r => new MovieResult(r.Title, r.Genre, r.Description, r.Score)));
    }
}
