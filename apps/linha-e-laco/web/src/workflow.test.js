import { describe, expect, it } from 'vitest'
import { addressOrNull, nextOrderStatus, receiptMessage, whatsappUrl } from './workflow'

describe('web workflow', () => {
  it('advances an order only through valid stages', () => {
    expect(nextOrderStatus('Quoted')).toBe('Approved')
    expect(nextOrderStatus('Ready')).toBe('Delivered')
    expect(nextOrderStatus('Delivered')).toBeNull()
  })
  it('keeps an empty address out of the client payload', () => {
    expect(addressOrNull({ street: '', city: '', state: '' })).toBeNull()
    expect(addressOrNull({ street: 'Rua das Flores', city: '', state: '' })).toEqual({ street: 'Rua das Flores', city: '', state: '' })
  })
  it('creates a safe WhatsApp receipt link', () => {
    const message = receiptMessage('LL-001', 'https://api.example/receipt')
    expect(whatsappUrl('(11) 99999-0000', message)).toContain('https://wa.me/11999990000?text=')
    expect(whatsappUrl('(11) 99999-0000', message)).toContain(encodeURIComponent('LL-001'))
  })
})