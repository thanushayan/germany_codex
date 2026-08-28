import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useForm, type UseFormRegisterReturn } from 'react-hook-form'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { authApi, getCurrentUser } from '../../api/auth'
import { useI18n, type Locale } from '../../i18n/I18nProvider'

const emailSchema = (invalidEmail: string) =>
  z.string().email(invalidEmail).max(256, invalidEmail)
const passwordSchema = (passwordRules: string) =>
  z
    .string()
    .min(12, passwordRules)
    .max(128, passwordRules)
    .regex(/[a-z]/, passwordRules)
    .regex(/[A-Z]/, passwordRules)
    .regex(/[0-9]/, passwordRules)
    .regex(/[^a-zA-Z0-9]/, passwordRules)

function AuthLayout({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  const { locale, messages, setLocale } = useI18n()
  return (
    <main className="mx-auto flex min-h-screen max-w-lg items-center px-6 py-12">
      <section className="w-full rounded-3xl bg-white p-8 shadow-sm ring-1 ring-slate-200">
        <div className="mb-8 flex items-center justify-between gap-4">
          <Link to="/" className="font-semibold text-blue-800">
            {messages.appName}
          </Link>
          <select
            aria-label={messages.locale}
            value={locale}
            onChange={(event) => setLocale(event.target.value as Locale)}
            className="rounded-lg border border-slate-300 px-3 py-2"
          >
            <option value="en">{messages.english}</option>
            <option value="ta">{messages.tamil}</option>
          </select>
        </div>
        <h1 className="text-3xl font-bold text-slate-950">{title}</h1>
        <div className="mt-7">{children}</div>
      </section>
    </main>
  )
}

function Field({
  label,
  type,
  error,
  registration,
  autoComplete,
}: {
  label: string
  type: 'email' | 'password'
  error?: string
  registration: UseFormRegisterReturn
  autoComplete?: 'email' | 'current-password' | 'new-password'
}) {
  const id = registration.name
  return (
    <div>
      <label
        htmlFor={id}
        className="block text-sm font-semibold text-slate-800"
      >
        {label}
      </label>
      <input
        id={id}
        type={type}
        autoComplete={
          autoComplete ?? (type === 'email' ? 'email' : 'current-password')
        }
        aria-invalid={Boolean(error)}
        aria-describedby={error ? `${id}-error` : undefined}
        className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-200"
        {...registration}
      />
      {error && (
        <p
          id={`${id}-error`}
          role="alert"
          className="mt-2 text-sm text-red-700"
        >
          {error}
        </p>
      )}
    </div>
  )
}

function SubmitButton({
  children,
  pending,
}: {
  children: ReactNode
  pending: boolean
}) {
  return (
    <button
      type="submit"
      disabled={pending}
      className="w-full rounded-xl bg-blue-700 px-4 py-3 font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
    >
      {children}
    </button>
  )
}

function ErrorMessage({ show }: { show: boolean }) {
  const { messages } = useI18n()
  return show ? (
    <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-800">
      {messages.authError}
    </p>
  ) : null
}

export function RegisterScreen() {
  const { locale, messages } = useI18n()
  const navigate = useNavigate()
  const schema = z.object({
    email: emailSchema(messages.invalidEmail),
    password: passwordSchema(messages.passwordRules),
  })
  type Values = z.infer<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema) })
  const mutation = useMutation({
    mutationFn: (values: Values) =>
      authApi.register({ ...values, preferredLocale: locale }),
    onSuccess: () => navigate('/verify-email'),
  })

  return (
    <AuthLayout title={messages.registerTitle}>
      <form
        className="space-y-5"
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        noValidate
      >
        <Field
          label={messages.email}
          type="email"
          registration={form.register('email')}
          error={form.formState.errors.email?.message}
        />
        <Field
          label={messages.password}
          type="password"
          autoComplete="new-password"
          registration={form.register('password')}
          error={form.formState.errors.password?.message}
        />
        <ErrorMessage show={mutation.isError} />
        <SubmitButton pending={mutation.isPending}>
          {messages.register}
        </SubmitButton>
      </form>
      <p className="mt-6 text-sm text-slate-600">
        {messages.haveAccount}{' '}
        <Link className="font-semibold text-blue-700 underline" to="/login">
          {messages.login}
        </Link>
      </p>
    </AuthLayout>
  )
}

export function LoginScreen() {
  const { messages } = useI18n()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const schema = z.object({
    email: emailSchema(messages.invalidEmail),
    password: z.string().min(1, messages.required).max(128),
  })
  type Values = z.infer<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema) })
  const mutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['current-user'] })
      const destination =
        (location.state as { from?: string } | null)?.from ?? '/account'
      navigate(destination, { replace: true })
    },
  })

  return (
    <AuthLayout title={messages.loginTitle}>
      <form
        className="space-y-5"
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        noValidate
      >
        <Field
          label={messages.email}
          type="email"
          registration={form.register('email')}
          error={form.formState.errors.email?.message}
        />
        <Field
          label={messages.password}
          type="password"
          registration={form.register('password')}
          error={form.formState.errors.password?.message}
        />
        <ErrorMessage show={mutation.isError} />
        <SubmitButton pending={mutation.isPending}>
          {messages.login}
        </SubmitButton>
      </form>
      <div className="mt-6 flex flex-wrap justify-between gap-3 text-sm">
        <Link
          className="font-semibold text-blue-700 underline"
          to="/forgot-password"
        >
          {messages.forgotPassword}
        </Link>
        <span>
          {messages.needAccount}{' '}
          <Link
            className="font-semibold text-blue-700 underline"
            to="/register"
          >
            {messages.register}
          </Link>
        </span>
      </div>
    </AuthLayout>
  )
}

export function ForgotPasswordScreen() {
  const { messages } = useI18n()
  const schema = z.object({ email: emailSchema(messages.invalidEmail) })
  type Values = z.infer<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema) })
  const mutation = useMutation({ mutationFn: authApi.forgotPassword })

  return (
    <AuthLayout title={messages.forgotTitle}>
      {mutation.isSuccess ? (
        <p role="status" className="rounded-xl bg-blue-50 p-4 text-blue-900">
          {messages.recoveryGeneric}
        </p>
      ) : (
        <form
          className="space-y-5"
          onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
          noValidate
        >
          <Field
            label={messages.email}
            type="email"
            registration={form.register('email')}
            error={form.formState.errors.email?.message}
          />
          <ErrorMessage show={mutation.isError} />
          <SubmitButton pending={mutation.isPending}>
            {messages.forgotSubmit}
          </SubmitButton>
        </form>
      )}
    </AuthLayout>
  )
}

function useTokenParameters() {
  const location = useLocation()
  return new URLSearchParams(location.hash.replace(/^#/, ''))
}

export function ResetPasswordScreen() {
  const { messages } = useI18n()
  const params = useTokenParameters()
  const navigate = useNavigate()
  const schema = z.object({
    newPassword: passwordSchema(messages.passwordRules),
  })
  type Values = z.infer<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema) })
  const userId = params.get('userId') ?? ''
  const code = params.get('code') ?? ''
  const mutation = useMutation({
    mutationFn: (values: Values) =>
      authApi.resetPassword({ userId, code, ...values }),
    onSuccess: () => navigate('/login', { replace: true }),
  })

  return (
    <AuthLayout title={messages.resetTitle}>
      <form
        className="space-y-5"
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        noValidate
      >
        <Field
          label={messages.newPassword}
          type="password"
          autoComplete="new-password"
          registration={form.register('newPassword')}
          error={form.formState.errors.newPassword?.message}
        />
        <ErrorMessage show={mutation.isError || !userId || !code} />
        <SubmitButton pending={mutation.isPending || !userId || !code}>
          {messages.resetSubmit}
        </SubmitButton>
      </form>
    </AuthLayout>
  )
}

export function VerifyEmailScreen() {
  const { messages } = useI18n()
  const params = useTokenParameters()
  const userId = params.get('userId') ?? ''
  const code = params.get('code') ?? ''
  const mutation = useMutation({
    mutationFn: () => authApi.verifyEmail({ userId, code }),
  })

  return (
    <AuthLayout title={messages.verifyTitle}>
      {mutation.isSuccess ? (
        <p role="status" className="rounded-xl bg-green-50 p-4 text-green-900">
          {messages.verificationSuccess}
        </p>
      ) : (
        <div className="space-y-5">
          <p className="text-slate-700">{messages.verificationPending}</p>
          <ErrorMessage show={mutation.isError} />
          {userId && code && (
            <button
              type="button"
              onClick={() => mutation.mutate()}
              className="w-full rounded-xl bg-blue-700 px-4 py-3 font-semibold text-white"
            >
              {messages.verifySubmit}
            </button>
          )}
        </div>
      )}
    </AuthLayout>
  )
}

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const location = useLocation()
  const { messages } = useI18n()
  const currentUser = useQuery({
    queryKey: ['current-user'],
    queryFn: ({ signal }) => getCurrentUser(signal),
    retry: false,
  })
  if (currentUser.isPending) return <p role="status">{messages.loading}</p>
  if (currentUser.isError) return <p role="alert">{messages.authError}</p>
  if (!currentUser.data)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return children
}

export function AccountScreen() {
  const { messages } = useI18n()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const currentUser = useQuery({
    queryKey: ['current-user'],
    queryFn: ({ signal }) => getCurrentUser(signal),
    retry: false,
  })
  const logout = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      queryClient.setQueryData(['current-user'], null)
      navigate('/login', { replace: true })
    },
  })
  return (
    <AuthLayout title={messages.account}>
      <p>{messages.protectedMessage}</p>
      <p className="mt-3 font-medium">{currentUser.data?.email}</p>
      <button
        type="button"
        onClick={() => logout.mutate()}
        className="mt-6 rounded-xl bg-slate-900 px-4 py-3 font-semibold text-white"
      >
        {messages.logout}
      </button>
    </AuthLayout>
  )
}
