import { useState } from 'react'
import './App.css'

export default function App() {
  const [mood,    setMood]    = useState('')
  const [results, setResults] = useState([])
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState(null)

  async function handleFind() {
    if (!mood.trim()) return

    setLoading(true)
    setError(null)
    setResults([])

    try {
      const res = await fetch('http://localhost:5000/api/recommend', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ mood }),
      })

      if (!res.ok) {
        const text = await res.text()
        throw new Error(text || `Server error: ${res.status}`)
      }

      const data = await res.json()
      setResults(data)
    } catch {
      setError('Could not reach the server. Make sure the API is running.')
    } finally {
      setLoading(false)
    }
  }

  function handleKeyDown(e) {
    if (e.key === 'Enter') handleFind()
  }

  return (
    <div className="container">
      <h1>🎬 Movie Mood Finder</h1>
      <p className="subtitle">Describe a vibe and we&apos;ll find your perfect film</p>

      <div className="search-row">
        <input
          type="text"
          value={mood}
          onChange={e => setMood(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="e.g. funny with a twist ending..."
          disabled={loading}
        />
        <button onClick={handleFind} disabled={loading || !mood.trim()}>
          {loading ? 'Finding...' : 'Find Movies'}
        </button>
      </div>

      {loading && <p className="loading">Finding movies…</p>}
      {error   && <p className="error">{error}</p>}

      {results.length > 0 && (
        <div className="results">
          {results.map((movie, i) => (
            <div key={i} className="card">
              <span className="card-rank">#{i + 1}</span>
              <h2>{movie.title}</h2>
              <span className="badge">{movie.genre}</span>
              <p className="description">{movie.description}</p>
              <div className="score-row">
                <span className="score-label">Match {Math.round(movie.score * 100)}%</span>
                <div className="score-bar-track">
                  <div className="score-bar-fill" style={{ width: `${Math.round(movie.score * 100)}%` }} />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
