import { z } from 'zod'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'
let csrfToken: string | null = null

const currentUserSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  preferredLocale: z.enum(['en', 'ta']),
  roles: z.array(z.string()),
})

export type CurrentUser = z.infer<typeof currentUserSchema>

type RequestOptions = {
  method?: 'GET' | 'POST'
  body?: unknown
  signal?: AbortSignal
}

async function getCsrfToken(signal?: AbortSignal) {
  if (csrfToken) return csrfToken
  const response = await fetch(`${apiBaseUrl}/api/auth/csrf`, {
    credentials: 'include',
    signal,
  })
  if (!response.ok) throw new Error('Unable to initialise a secure request.')
  const payload = z
    .object({ token: z.string().min(1) })
    .parse(await response.json())
  csrfToken = payload.token
  return csrfToken
}

async function request(path: string, options: RequestOptions = {}) {
  const headers = new Headers({ Accept: 'application/json' })
  if (options.body !== undefined)
    headers.set('Content-Type', 'application/json')
  if (options.method === 'POST')
    headers.set('X-CSRF-TOKEN', await getCsrfToken(options.signal))

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method ?? 'GET',
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    credentials: 'include',
    headers,
    signal: options.signal,
  })

  if (!response.ok) throw new Error('The request could not be completed.')
  return response
}

export async function getCurrentUser(
  signal?: AbortSignal,
): Promise<CurrentUser | null> {
  const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
    credentials: 'include',
    headers: { Accept: 'application/json' },
    signal,
  })
  if (response.status === 401) return null
  if (!response.ok) throw new Error('Unable to load the current account.')
  return currentUserSchema.parse(await response.json())
}

export const authApi = {
  register: (body: {
    email: string
    password: string
    preferredLocale: 'en' | 'ta'
  }) => request('/api/auth/register', { method: 'POST', body }),
  login: (body: { email: string; password: string }) =>
    request('/api/auth/login', { method: 'POST', body }),
  logout: () => request('/api/auth/logout', { method: 'POST' }),
  forgotPassword: (body: { email: string }) =>
    request('/api/auth/forgot-password', { method: 'POST', body }),
  resetPassword: (body: {
    userId: string
    code: string
    newPassword: string
  }) => request('/api/auth/reset-password', { method: 'POST', body }),
  verifyEmail: (body: { userId: string; code: string }) =>
    request('/api/auth/verify-email', { method: 'POST', body }),
}
