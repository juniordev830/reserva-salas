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

var sala = new ReservaService();
var inicio2 = new DateTime(2026, 11, 3, 9, 0, 0);
var a = sala.Reservar(" Sala A ", inicio2, inicio2.AddHours(1));
var b = sala.Reservar("sala a", inicio2.AddHours(1), inicio2.AddHours(2));
var c = sala.Reservar("Sala B", inicio2, inicio2.AddHours(1));
if (sala.Disponivel("Sala A", inicio2, inicio2.AddMinutes(30)) ||
    !sala.Disponivel("Sala B", inicio2.AddHours(1), inicio2.AddHours(2)))
    throw new Exception("A consulta de disponibilidade deve isolar os recursos e respeitar conflitos.");

var snapshot = sala.Listar();
try
{
    sala.Reagendar(a.Id, inicio2.AddHours(1), inicio2.AddHours(2));
    throw new Exception("Remarcação com conflito deveria falhar.");
}
catch (InvalidOperationException) { }
if (!sala.Listar().Contains(a) || sala.Disponivel("Sala A", inicio2, inicio2.AddHours(1)))
    throw new Exception("O período original deve ser preservado após falha.");

var remarcado = sala.Reagendar(a.Id, inicio2.AddHours(3), inicio2.AddHours(4));
if (remarcado.Id != a.Id || !sala.Disponivel("Sala A", inicio2, inicio2.AddHours(1)))
    throw new Exception("Remarcação deve preservar o ID e liberar o intervalo anterior.");
if (snapshot.Count != 3 || !snapshot.Contains(a) || snapshot.Contains(remarcado))
    throw new Exception("Listar deve retornar snapshot estável.");
if (!sala.ListarRecurso("SALA A").Select(x => x.Id).SequenceEqual(new[] { b.Id, a.Id }))
    throw new Exception("Recurso deve ser filtrado sem distinguir caixa e ordenado por hora.");

try
{
    sala.Reagendar(9999, inicio2, inicio2.AddHours(1));
    throw new Exception("Não deve remarcar reserva inexistente.");
}
catch (KeyNotFoundException) { }
try
{
    sala.Reagendar(a.Id, inicio2, inicio2);
    throw new Exception("Não deve aceitar intervalo inválido.");
}
catch (ArgumentException) { }

var concorrente = new ReservaService();
var start = new System.Threading.ManualResetEventSlim(false);
var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
{
    start.Wait();
    try { concorrente.Reservar("Sala C", inicio2, inicio2.AddHours(1)); return true; }
    catch (InvalidOperationException) { return false; }
})).ToArray();
start.Set();
if (!Task.WaitAll(tasks, TimeSpan.FromSeconds(10)))
    throw new Exception("Teste concorrente excedeu o tempo.");
if (tasks.Count(t => t.Result) != 1 || concorrente.Listar().Count != 1)
    throw new Exception("Operações concorrentes não devem reservar o mesmo período.");

Console.WriteLine("Reserva Salas: smoke tests OK");
