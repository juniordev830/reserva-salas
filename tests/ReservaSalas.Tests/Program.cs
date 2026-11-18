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

Console.WriteLine("Reserva Salas: smoke tests OK");
