import { useQuery } from '@tanstack/react-query'
import { Link, Route, Routes } from 'react-router-dom'
import { getApiHealth } from '../api/health'

function HomePage() {
  const health = useQuery({
    queryKey: ['api-health'],
    queryFn: ({ signal }) => getApiHealth(signal),
    retry: false,
  })

  const healthText = health.isPending
    ? 'Checking API…'
    : health.isSuccess
      ? 'API available'
      : 'API unavailable'

  return (
    <main className="mx-auto flex min-h-screen max-w-4xl items-center px-6 py-16">
      <section className="w-full rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200 sm:p-12">
        <p className="mb-3 text-sm font-semibold uppercase tracking-widest text-blue-700">
          Initial platform scaffold
        </p>
        <h1 className="text-4xl font-bold tracking-tight text-slate-950 sm:text-5xl">
          Germany Applications
        </h1>
        <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-600">
          A self-service application planning platform for Sri Lankan students exploring verified
          German Master&apos;s programmes.
        </p>
        <div className="mt-8 flex flex-wrap items-center gap-4">
          <span
            aria-live="polite"
            className="rounded-full bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700"
          >
            {healthText}
          </span>
          <Link className="text-sm font-semibold text-blue-700 underline-offset-4 hover:underline" to="/about">
            About this scaffold
          </Link>
        </div>
      </section>
    </main>
  )
}

function AboutPage() {
  return (
    <main className="mx-auto max-w-3xl px-6 py-16">
      <Link className="text-blue-700 underline" to="/">
        Back to home
      </Link>
      <h1 className="mt-8 text-3xl font-bold">About this scaffold</h1>
      <p className="mt-4 leading-7 text-slate-700">
        This route verifies that React Router is configured. Product features will be added only in
        approved development phases.
      </p>
    </main>
  )
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/about" element={<AboutPage />} />
    </Routes>
  )
}
