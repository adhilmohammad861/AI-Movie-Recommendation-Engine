# Movie Recommendation Engine

A full-stack AI-powered movie recommendation app. Describe a mood or vibe, and the engine uses **OpenAI embeddings** + **pgvector** cosine similarity to find the three most matching films from a curated library of 30 movies.

---

## Tech Stack

| Layer      | Technology                                    |
|------------|-----------------------------------------------|
| Frontend   | React 18 + Vite 5                             |
| Backend    | .NET 10 Web API (C#)                          |
| Database   | PostgreSQL + **pgvector** extension           |
| AI Model   | OpenAI `text-embedding-3-small` (1536 dims)   |
| ORM        | Entity Framework Core 9 + Npgsql              |

---

## How to Run

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL with the [pgvector extension](https://github.com/pgvector/pgvector) installed
- An [OpenAI API key](https://platform.openai.com/api-keys)

---

### 1. Database Setup

Open **pgAdmin** (or any PostgreSQL client) and run:

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS movies (
  id          SERIAL PRIMARY KEY,
  title       TEXT NOT NULL,
  genre       TEXT NOT NULL,
  description TEXT NOT NULL,
  embedding   vector(1536),
  created_at  TIMESTAMPTZ DEFAULT NOW()
);
```

---

### 2. Configure the Backend

Open `MovieApi/appsettings.Development.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=moviedb;Username=postgres;Password=YOUR_PASSWORD_HERE"
  },
  "OpenAI": {
    "ApiKey": "sk-YOUR_KEY_HERE"
  }
}
```

> `appsettings.Development.json` is in `.gitignore` — your secrets are never committed.

---

### 3. Start the Backend

```bash
cd MovieRecommendationEngine/MovieApi
dotnet restore
dotnet run
# API is now running at http://localhost:5000
```

---

### 4. Seed the Database (run once)

Send a `POST` request to embed all 30 movies and store them with their vectors:

```bash
# Using curl:
curl -X POST http://localhost:5000/api/seed

# Or open Postman and POST to http://localhost:5000/api/seed
```

Expected response: `{ "seededCount": 30 }`  
Running it again is safe — duplicate titles are skipped: `{ "seededCount": 0 }`

---

### 5. Start the Frontend

```bash
cd MovieRecommendationEngine/movie-ui
npm install
npm run dev
# Opens at http://localhost:5173
```

---

### 6. Use the App

Go to **http://localhost:5173**, type a mood or vibe (e.g. *"funny with a twist ending"*), and click **Find Movies**. The top 3 matching films with similarity scores appear as cards.

---

## How pgvector Works in This App

### What is an embedding?

An **embedding** is a list of ~1536 numbers that represents the *meaning* of a piece of text — not the words themselves, but the concept behind them. OpenAI's `text-embedding-3-small` model turns any string (a movie description, a mood phrase) into one of these numeric vectors. Two texts that mean similar things will produce vectors that are mathematically close together.

### Why do we store embeddings?

When you run `POST /api/seed`, the app calls the OpenAI API for each of the 30 movie descriptions and stores the resulting `float[1536]` vector in the `embedding` column (PostgreSQL `vector(1536)` type via pgvector). This is a one-time cost — once stored, we query instantly without calling OpenAI again for each search.

### What does `<=>` do?

`<=>` is pgvector's **cosine distance** operator. Cosine distance measures the angle between two vectors: 0 means identical direction (same meaning), 2 means opposite. So:

```
similarity = 1 - cosine_distance = 1 - (embedding <=> queryVector)
```

A score of 0.95 means the movie's vibe is 95% aligned with your input.

### Why is this better than keyword search?

| Keyword search | Embedding search |
|----------------|-----------------|
| Needs exact word matches | Matches by *meaning*, not words |
| "sad breakup film" ≠ "divorce drama" | These score high similarity |
| Brittle — typos and synonyms fail | Robust to paraphrase and synonyms |
| Can't capture abstract mood ("cozy rainy day") | Embeddings encode abstract concepts perfectly |

Instead of asking *"does this description contain the word 'funny'?"*, pgvector asks *"is the geometry of this description close to the geometry of your mood?"* — which is a fundamentally more powerful question.
