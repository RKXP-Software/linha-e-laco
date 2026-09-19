import { useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'
import './navigation.css'

const api = import.meta.env.VITE_API_URL ?? 'http://localhost:5178/api'
const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const status = { Quoted: 'Orçamentos', Approved: 'Aprovados', InProduction: 'Produção', Ready: 'Prontos', Delivered: 'Entregues', Cancelled: 'Cancelados' }

function Metric({ label, value, hint }) { return <article className="metric"><span>{label}</span><strong>{value}</strong>{hint && <small>{hint}</small>}</article> }
function App() {
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const load = async () => { try { const response = await fetch(`${api}/dashboard`); if (!response.ok) throw new Error('Não foi possível carregar as métricas.'); setData(await response.json()) } catch (cause) { setError(cause.message) } }
  useEffect(() => { load() }, [])
  return <main><aside><p className="brand">Linha & Laço</p><h1>Back-office</h1><nav aria-label="Seções planejadas"><span className="active">Visão geral</span><span>Catálogo</span><span>Clientes</span><span>Configurações</span></nav></aside><section className="content"><header><div><p className="eyebrow">Últimos 30 dias</p><h2>Um ateliê que você consegue enxergar.</h2></div><button onClick={load}>Atualizar dados</button></header>{error && <p className="error">{error}</p>}{data && <><section className="metrics"><Metric label="Recebido" value={money.format(data.received)}/><Metric label="Previsto" value={money.format(data.expected)}/><Metric label="Em atraso" value={money.format(data.overdue)}/><Metric label="Ticket médio" value={money.format(data.averageTicket)}/></section><section className="panels"><article><p className="eyebrow">Pipeline</p><h3>Pedidos por etapa</h3><div className="rows">{Object.entries(data.ordersByStatus).map(([key, count]) => <div key={key}><span>{status[key] ?? key}</span><b>{count}</b></div>)}</div></article><article><p className="eyebrow">Itens em destaque</p><h3>Mais vendidos</h3><div className="rows">{data.bestSellers.map(item => <div key={item.name}><span>{item.name}</span><b>{item.quantity} un.</b></div>)}</div></article></section><footer><span>{data.orders} pedidos no período · {data.activeClients} clientes ativos</span><b>{data.quoteConversion}% de conversão de orçamento</b></footer></>}</section></main>
}
createRoot(document.getElementById('root')).render(<App />)
