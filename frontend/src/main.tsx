import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';
type Room = { id: number; name: string; capacity: number; active: boolean };
type Booking = { id: number; roomId: number; startsAt: string; endsAt: string; ownerId: string };
const API = import.meta.env.VITE_API_URL || 'http://localhost:8080';
function App() {
 const [token,setToken]=useState(sessionStorage.getItem('token')||'');
 const [rooms,setRooms]=useState<Room[]>([]); const [bookings,setBookings]=useState<Booking[]>([]);
 const [room,setRoom]=useState(''); const [start,setStart]=useState(''); const [end,setEnd]=useState('');
 const [error,setError]=useState(''); const [busy,setBusy]=useState(false);
 async function request(path:string, options:RequestInit={}) {
  const res=await fetch(API+path,{...options,headers:{'Content-Type':'application/json',Authorization:'Bearer '+token,...options.headers}});
  if(!res.ok) throw new Error('HTTP '+res.status+': '+await res.text());
  return res.status===204?null:res.json();
 }
 async function refresh(){try{const [r,b]=await Promise.all([request('/api/rooms'),request('/api/bookings')]);setRooms(r);setBookings(b);setError('');}catch(e){setError(String(e));}}
 useEffect(()=>{if(token)void refresh();},[token]);
 async function reserve(e:React.FormEvent){e.preventDefault();setBusy(true);try{await request('/api/bookings',{method:'POST',body:JSON.stringify({roomId:Number(room),startsAt:new Date(start).toISOString(),endsAt:new Date(end).toISOString()})});await refresh();}catch(e){setError(String(e));}finally{setBusy(false);}}
 async function cancel(id:number){if(!confirm('Cancelar esta reserva?'))return;try{await request('/api/bookings/'+id,{method:'DELETE'});await refresh();}catch(e){setError(String(e));}}
 return <main><header><h1>Reserva Salas</h1><p>Gestão de espaços e equipamentos</p></header><section><label>Token JWT de acesso <input aria-label="Token JWT" type="password" value={token} onChange={e=>{setToken(e.target.value);sessionStorage.setItem('token',e.target.value);}} placeholder="Cole um token emitido pelo provedor de identidade"/></label><p className="hint">A autenticação depende de um emissor JWT configurado no servidor.</p></section>{error&&<p role="alert" className="error">{error}</p>}<section><h2>Nova reserva</h2><form onSubmit={reserve}><label>Sala<select required value={room} onChange={e=>setRoom(e.target.value)}><option value="">Selecione</option>{rooms.filter(r=>r.active).map(r=><option key={r.id} value={r.id}>{r.name} · {r.capacity} pessoas</option>)}</select></label><label>Início<input type="datetime-local" required value={start} onChange={e=>setStart(e.target.value)}/></label><label>Fim<input type="datetime-local" required value={end} onChange={e=>setEnd(e.target.value)}/></label><button disabled={busy}>Reservar</button></form></section><section><h2>Reservas</h2>{bookings.length===0?<p>Nenhuma reserva cadastrada.</p>:<ul>{bookings.map(b=><li key={b.id}><span><strong>{rooms.find(r=>r.id===b.roomId)?.name||'Sala '+b.roomId}</strong><small>{new Date(b.startsAt).toLocaleString('pt-BR')} — {new Date(b.endsAt).toLocaleString('pt-BR')}</small></span><button onClick={()=>void cancel(b.id)}>Cancelar</button></li>)}</ul>}<button className="secondary" onClick={()=>void refresh()}>Atualizar</button></section></main>;
}
createRoot(document.getElementById('root')!).render(<React.StrictMode><App/></React.StrictMode>);