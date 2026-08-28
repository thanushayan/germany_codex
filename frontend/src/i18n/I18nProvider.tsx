import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authEn } from './en/auth'
import { authTa } from './ta/auth'

export type Locale = 'en' | 'ta'
type Messages = { [Key in keyof typeof authEn]: string }

const I18nContext = createContext<{
  locale: Locale
  messages: Messages
  setLocale: (locale: Locale) => void
} | null>(null)

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>('en')
  const value = useMemo(() => {
    const messages: Messages = locale === 'ta' ? authTa : authEn
    return {
      locale,
      messages,
      setLocale: (nextLocale: Locale) => {
        document.documentElement.lang = nextLocale
        setLocaleState(nextLocale)
      },
    }
  }, [locale])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n() {
  const value = useContext(I18nContext)
  if (!value) throw new Error('useI18n must be used within I18nProvider')
  return value
}
