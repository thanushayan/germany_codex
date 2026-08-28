import { useQuery } from '@tanstack/react-query'
import { Link, Route, Routes } from 'react-router-dom'
import { getApiHealth } from '../api/health'
import {
  AccountScreen,
  ForgotPasswordScreen,
  LoginScreen,
  ProtectedRoute,
  RegisterScreen,
  ResetPasswordScreen,
  VerifyEmailScreen,
} from '../features/auth/AuthScreens'
import { useI18n } from '../i18n/I18nProvider'

function HomePage() {
  const { messages } = useI18n()
  const health = useQuery({
    queryKey: ['api-health'],
    queryFn: ({ signal }) => getApiHealth(signal),
    retry: false,
  })
  const healthText = health.isPending
    ? messages.apiChecking
    : health.isSuccess
      ? messages.apiAvailable
      : messages.apiUnavailable
  return (
    <main className="mx-auto flex min-h-screen max-w-4xl items-center px-6 py-16">
      <section className="w-full rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200 sm:p-12">
        <p className="mb-3 text-sm font-semibold uppercase tracking-widest text-blue-700">
          {messages.secureAccess}
        </p>
        <h1 className="text-4xl font-bold tracking-tight text-slate-950 sm:text-5xl">
          {messages.appName}
        </h1>
        <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-600">
          {messages.homeDescription}
        </p>
        <p
          aria-live="polite"
          className="mt-6 text-sm font-medium text-slate-600"
        >
          {healthText}
        </p>
        <div className="mt-8 flex flex-wrap gap-4">
          <Link
            className="rounded-xl bg-blue-700 px-5 py-3 font-semibold text-white"
            to="/register"
          >
            {messages.register}
          </Link>
          <Link
            className="rounded-xl border border-slate-300 px-5 py-3 font-semibold text-slate-800"
            to="/login"
          >
            {messages.login}
          </Link>
        </div>
      </section>
    </main>
  )
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/register" element={<RegisterScreen />} />
      <Route path="/login" element={<LoginScreen />} />
      <Route path="/forgot-password" element={<ForgotPasswordScreen />} />
      <Route path="/reset-password" element={<ResetPasswordScreen />} />
      <Route path="/verify-email" element={<VerifyEmailScreen />} />
      <Route
        path="/account"
        element={
          <ProtectedRoute>
            <AccountScreen />
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}
