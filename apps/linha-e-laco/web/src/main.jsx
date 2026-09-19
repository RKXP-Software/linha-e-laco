import { useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'
import './notes.css'

const api = import.meta.env.VITE_API_URL ?? 'http://localhost:5178/api'
const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const label = { Quoted: 'Orçamento', Approved: 'Aprovado', InProduction: 'Em produção', Ready: 'Pronto', Delivered: 'Entregue', Cancelled: 'Cancelado' }

function App() {
  const [clients, setClients] = useState([])
  const [orders, setOrders] = useState([])
  const [notes, setNotes] = useState([])
  const [error, setError] = useState('')
  const [form, setForm] = useState({ name: '', phone: '' })
  const [noteText, setNoteText] = useState('')

  const load = async () => {
    try {
      setError('')
      const [clientResponse, orderResponse, noteResponse] = await Promise.all([fetch(`${api}/clients`), fetch(`${api}/orders`), fetch(`${api}/notes`)])
      if (!clientResponse.ok || !orderResponse.ok || !noteResponse.ok) throw new Error('Não foi possível carregar os dados.')
      setClients(await clientResponse.json())
      setOrders(await orderResponse.json())
      setNotes(await noteResponse.json())
    } catch (cause) { setError(cause.message) }
  }
  useEffect(() => { load() }, [])
  const addClient = async event => {
    event.preventDefault()
    const response = await fetch(`${api}/clients`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) })
    if (!response.ok) return setError('Informe ao menos o nome do cliente.')
    setError(''); setForm({ name: '', phone: '' }); load()
  }
  const addNote = async event => {
    event.preventDefault()
    const response = await fetch(`${api}/notes`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text: noteText }) })
    if (!response.ok) return setError('Escreva uma anotação antes de salvar.')
    setError(''); setNoteText(''); load()
  }
  const toggleNote = async note => { await fetch(`${api}/notes/${note.id}`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ isDone: !note.isDone }) }); load() }
  const deleteNote = async id => { await fetch(`${api}/notes/${id}`, { method: 'DELETE' }); load() }
  return <main>
    <header><div><p className="eyebrow">Linha & Laço</p><h1>Ateliê em movimento.</h1></div><button onClick={load}>Atualizar</button></header>
    {error && <p className="error">{error}</p>}
    <section className="summary"><article><strong>{clients.length}</strong><span>clientes</span></article><article><strong>{orders.length}</strong><span>pedidos ativos</span></article></section>
    <section className="grid"><article className="card"><p className="eyebrow">Novo cliente</p><h2>Comece pelo cuidado.</h2><form onSubmit={addClient}><input placeholder="Nome completo" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })}/><input placeholder="Telefone" value={form.phone} onChange={event => setForm({ ...form, phone: event.target.value })}/><button>Cadastrar cliente</button></form></article>
      <article className="card"><p className="eyebrow">Pedidos</p><h2>Próximos trabalhos</h2><div className="list">{orders.map(order => <div key={order.id}><span><b>{order.number}</b><small>{order.items.map(item => item.nameSnapshot).join(', ')}</small></span><span className="status">{label[order.status]}</span><b>{money.format(order.total)}</b></div>)}</div></article></section>
    <section className="card notes-card"><p className="eyebrow">Anotações</p><h2>Não deixe um pedido escapar.</h2><form className="note-form" onSubmit={addNote}><input aria-label="Nova anotação" placeholder="Ex.: ligar para cliente sobre prova na quinta" value={noteText} onChange={event => setNoteText(event.target.value)}/><button>Salvar anotação</button></form><div className="notes">{notes.map(note => <div className={note.isDone ? 'note done' : 'note'} key={note.id}><label><input type="checkbox" checked={note.isDone} onChange={() => toggleNote(note)}/><span>{note.text}</span></label><button type="button" className="delete" aria-label={`Excluir anotação: ${note.text}`} onClick={() => deleteNote(note.id)}>Excluir</button></div>)}{notes.length === 0 && <small>Nenhuma anotação por aqui.</small>}</div></section>
  </main>
}
createRoot(document.getElementById('root')).render(<App />)
