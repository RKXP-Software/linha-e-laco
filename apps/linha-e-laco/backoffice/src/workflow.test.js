import { describe, expect, it } from 'vitest'
import { nextOrderStatus, pendingInstallments } from './workflow'

describe('back-office workflow', () => {
  it('does not advance delivered or cancelled orders', () => {
    expect(nextOrderStatus('Delivered')).toBeNull()
    expect(nextOrderStatus('Cancelled')).toBeNull()
  })
  it('shows only installments still awaiting payment', () => {
    expect(pendingInstallments([{ id: '1', status: 'Paid' }, { id: '2', status: 'Open' }, { id: '3', status: 'Overdue' }]).map(item => item.id)).toEqual(['2', '3'])
  })
})