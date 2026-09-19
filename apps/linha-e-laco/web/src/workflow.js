export const nextOrderStatus = status => ({ Quoted: 'Approved', Approved: 'InProduction', InProduction: 'Ready', Ready: 'Delivered' }[status] ?? null)
export const addressOrNull = address => Object.values(address ?? {}).some(value => String(value ?? '').trim()) ? address : null
export const whatsappUrl = (phone, message) => `https://wa.me/${String(phone ?? '').replace(/\D/g, '')}?text=${encodeURIComponent(message)}`
export const receiptMessage = (orderNumber, receiptUrl) => `Olá! Segue o recibo do pedido ${orderNumber}: ${receiptUrl}`