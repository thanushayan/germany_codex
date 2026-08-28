import { z } from 'zod'

const healthResponseSchema = z.object({
  status: z.string(),
})

export type HealthResponse = z.infer<typeof healthResponseSchema>

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

export async function getApiHealth(
  signal?: AbortSignal,
): Promise<HealthResponse> {
  const response = await fetch(`${apiBaseUrl}/health/live`, {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!response.ok) {
    throw new Error('The API health check is unavailable.')
  }

  return healthResponseSchema.parse(await response.json())
}
