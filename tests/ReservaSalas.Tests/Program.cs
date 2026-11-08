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

service.Cancelar(reserva.Id);
if (service.Listar().Count != 1)
    throw new Exception("O cancelamento deve remover apenas a reserva indicada.");

Console.WriteLine("Reserva Salas: smoke tests OK");

// Execute the expanded, dependency-free test suite.
AgendaSalasTests.Executar();
