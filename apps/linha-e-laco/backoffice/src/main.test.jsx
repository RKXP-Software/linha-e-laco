/** @vitest-environment jsdom */
import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { App } from './main'

const payload = path => path.endsWith('/dashboard') ? { expected: 0, ordersByStatus: {}, bestSellers: [], received: 0, overdue: 0, averageTicket: 0, orders: 0, activeClients: 0, quoteConversion: 0 } : []
afterEach(() => vi.unstubAllGlobals())
describe('back-office application', () => {
  it('switches from metrics to catalog management', async () => {
    vi.stubGlobal('fetch', vi.fn(url => Promise.resolve({ ok: true, status: 200, json: async () => payload(String(url)) })))
    render(<App />)
    expect(await screen.findByText('Ticket médio')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Catálogo' }))
    expect(screen.getByRole('heading', { name: 'Novo item' })).toBeTruthy()
  })
})