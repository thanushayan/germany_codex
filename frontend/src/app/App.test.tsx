import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '../i18n/I18nProvider'
import { App } from './App'

function renderApp(route: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <I18nProvider>
        <MemoryRouter initialEntries={[route]}>
          <App />
        </MemoryRouter>
      </I18nProvider>
    </QueryClientProvider>,
  )
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('authentication screens', () => {
  it('renders the login screen without exposing token storage', () => {
    renderApp('/login')

    expect(
      screen.getByRole('heading', { name: 'Sign in to your account' }),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Email address')).toHaveAttribute(
      'autocomplete',
      'email',
    )
    expect(screen.getByLabelText('Password')).toHaveAttribute(
      'type',
      'password',
    )
  })

  it('shows the same generic forgot-password result', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
    fetchMock
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200 }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 202 }))
    const user = userEvent.setup()
    renderApp('/forgot-password')

    await user.type(
      screen.getByLabelText('Email address'),
      'student@example.test',
    )
    await user.click(
      screen.getByRole('button', { name: 'Send reset instructions' }),
    )

    expect(await screen.findByRole('status')).toHaveTextContent(
      'If the account exists, password reset instructions will be sent.',
    )
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({
      credentials: 'include',
    })
  })

  it('redirects an anonymous visitor from a protected route to login', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(null, { status: 401 }),
    )
    renderApp('/account')

    expect(
      await screen.findByRole('heading', { name: 'Sign in to your account' }),
    ).toBeInTheDocument()
  })
})
