using ReservaSalas;

var service = new ReservaService();
var inicio = new DateTime(2026, 11, 3, 9, 0, 0);
var reserva = service.Reservar("Sala A", inicio, inicio.AddHours(2));

var adjacente = service.Reservar("Sala A", inicio.AddHours(2), inicio.AddHours(3));
if (adjacente.Id == reserva.Id)
    throw new Exception("Reservas devem receber identificadores diferentes.");

try
{
    service.Reservar("Sala A", inicio.AddHours(1), inicio.AddHours(4));
    throw new Exception("Uma reserva sobreposta deveria ser rejeitada.");
}
catch (InvalidOperationException)
{
    // comportamento esperado
}

var snapshot = service.Listar();
service.Cancelar(reserva.Id);
if (service.Listar().Count != 1)
    throw new Exception("O cancelamento deve remover apenas a reserva indicada.");
if (snapshot.Count != 2)
    throw new Exception("Uma consulta concluída não deve mudar depois de um cancelamento.");

var concorrente = new ReservaService();
var aceitas = 0;
Parallel.For(0, 64, _ =>
{
    try
    {
        concorrente.Reservar("Sala B", inicio, inicio.AddHours(1));
        Interlocked.Increment(ref aceitas);
    }
    catch (InvalidOperationException)
    {
        // Apenas uma chamada deve conseguir reservar esse período.
    }
});
if (aceitas != 1 || concorrente.Listar().Count != 1)
    throw new Exception("Reservas sobrepostas não podem ser aceitas em paralelo.");

Parallel.For(0, 64, index =>
{
    var horario = inicio.AddHours(index);
    concorrente.Reservar("Sala C", horario, horario.AddHours(1));
});
var reservas = concorrente.Listar();
if (reservas.Count != 65 || reservas.Select(item => item.Id).Distinct().Count() != reservas.Count)
    throw new Exception("Reservas simultâneas sem conflito devem receber identificadores distintos.");

var agendas = new ReservaService();
var primeira = agendas.Reservar("Sala A", inicio, inicio.AddHours(1));
var segunda = agendas.Reservar("Sala B", inicio, inicio.AddHours(1));
try
{
    agendas.Reagendar(primeira.Id, "sala b", inicio, inicio.AddHours(1));
    throw new Exception("Um reagendamento não pode tomar um horário ocupado.");
}
catch (InvalidOperationException)
{
    if (!agendas.Listar().SequenceEqual(new[] { primeira, segunda }))
        throw new Exception("Um reagendamento rejeitado deve conservar as duas reservas.");
}
var mesma = agendas.Reagendar(primeira.Id, "Sala A", inicio, inicio.AddHours(1));
if (mesma != primeira)
    throw new Exception("O próprio horário não deve causar conflito.");
var reagendada = agendas.Reagendar(primeira.Id, "Sala B", inicio.AddHours(1), inicio.AddHours(2));
if (reagendada.Id != primeira.Id || agendas.Listar()[0] != reagendada)
    throw new Exception("Reagendar deve manter o ID e aceitar horários adjacentes.");
agendas.Reservar("Sala A", inicio, inicio.AddHours(1));

var calendario = new ReservaService();
var tarde = calendario.Reservar("Auditório", inicio.AddHours(2), inicio.AddHours(3));
var manha = calendario.Reservar("  Auditório  ", inicio, inicio.AddHours(1));
var outroRecurso = calendario.Reservar("Sala B", inicio.AddMinutes(30), inicio.AddHours(2));
try
{
    calendario.Reservar("AUDITÓRIO", inicio.AddMinutes(15), inicio.AddMinutes(45));
    throw new Exception("A mesma sala com acentuação Unicode equivalente deve manter a exclusividade.");
}
catch (InvalidOperationException)
{
    // comportamento esperado
}
if (manha.Recurso != tarde.Recurso)
    throw new Exception("A mesma sala deve ter um nome normalizado.");

var periodo = calendario.ListarNoPeriodo(inicio.AddMinutes(30), inicio.AddHours(2));
if (!periodo.SequenceEqual(new[] { manha, outroRecurso }))
    throw new Exception("A consulta deve retornar sobreposições ordenadas e excluir reservas adjacentes.");
var filtradas = calendario.ListarNoPeriodo(inicio, inicio.AddHours(4), "  Auditório  ");
if (!filtradas.SequenceEqual(new[] { manha, tarde }))
    throw new Exception("O filtro de sala deve usar a mesma normalização da reserva.");
calendario.Cancelar(manha.Id);
if (filtradas.Count != 2)
    throw new Exception("Uma consulta concluída deve conservar seus resultados.");
try
{
    calendario.ListarNoPeriodo(inicio, inicio);
    throw new Exception("Um intervalo vazio deve ser rejeitado.");
}
catch (ArgumentException)
{
    // comportamento esperado
}

var mapa = new ReservaService();
mapa.Reservar("Auditório", inicio.AddMinutes(-30), inicio.AddMinutes(30));
mapa.Reservar("Auditório", inicio.AddHours(1), inicio.AddHours(2));
mapa.Reservar("Auditório", inicio.AddHours(2), inicio.AddHours(2.5));
mapa.Reservar("Auditório", inicio.AddHours(4), inicio.AddHours(5));
mapa.Reservar("Sala B", inicio.AddHours(3), inicio.AddHours(4));
var janelas = mapa.ListarDisponibilidade("  AUDITÓRIO ", inicio, inicio.AddHours(4.5), TimeSpan.FromHours(1));
if (!janelas.SequenceEqual(new[] { new IntervaloLivre(inicio.AddHours(2.5), inicio.AddHours(4)) }))
    throw new Exception("A disponibilidade deve respeitar o recurso, as bordas e a duração mínima.");
var livre = mapa.ListarDisponibilidade("Sala sem reservas", inicio, inicio.AddHours(1), TimeSpan.FromHours(1));
if (!livre.SequenceEqual(new[] { new IntervaloLivre(inicio, inicio.AddHours(1)) }))
    throw new Exception("Um recurso sem reservas deve poder usar a janela inteira.");
try
{
    mapa.ListarDisponibilidade("Auditório", inicio, inicio.AddHours(1), TimeSpan.Zero);
    throw new Exception("A duração mínima deve ser positiva.");
}
catch (ArgumentOutOfRangeException)
{
    // comportamento esperado
}
Console.WriteLine("Reserva Salas: smoke tests OK");
