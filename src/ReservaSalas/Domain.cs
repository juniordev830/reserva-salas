using System.Text;

namespace ReservaSalas;

public sealed record Reserva(int Id, string Recurso, DateTime Inicio, DateTime Fim)
{
    public static Reserva Criar(int id, string recurso, DateTime inicio, DateTime fim)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(recurso)) throw new ArgumentException("Recurso obrigatório.", nameof(recurso));
        if (fim <= inicio) throw new ArgumentException("O fim deve ser posterior ao início.", nameof(fim));
        return new Reserva(id, NormalizarRecurso(recurso), inicio, fim);
    }

    internal static string NormalizarRecurso(string recurso) =>
        recurso.Trim().Normalize(NormalizationForm.FormC);

    public bool Conflita(DateTime inicio, DateTime fim) =>
        inicio < Fim && fim > Inicio;
}

public sealed class ReservaService
{
    private readonly object _gate = new();
    private readonly List<Reserva> _reservas = [];
    private int _nextId = 1;

    public Reserva Reservar(string recurso, DateTime inicio, DateTime fim)
    {
        lock (_gate)
        {
            var proposta = Reserva.Criar(_nextId, recurso, inicio, fim);
            VerificarDisponibilidade(proposta);

            _reservas.Add(proposta);
            _nextId++;
            return proposta;
        }
    }

    public Reserva Reagendar(int id, string recurso, DateTime inicio, DateTime fim)
    {
        var proposta = Reserva.Criar(id, recurso, inicio, fim);
        lock (_gate)
        {
            var index = _reservas.FindIndex(reserva => reserva.Id == id);
            if (index < 0)
                throw new KeyNotFoundException($"Reserva {id} não encontrada.");

            VerificarDisponibilidade(proposta);
            _reservas[index] = proposta;
            return proposta;
        }
    }

    private void VerificarDisponibilidade(Reserva proposta)
    {
        if (_reservas.Any(reserva => reserva.Id != proposta.Id
                && reserva.Recurso.Equals(proposta.Recurso, StringComparison.OrdinalIgnoreCase)
                && reserva.Conflita(proposta.Inicio, proposta.Fim)))
            throw new InvalidOperationException("O recurso já está reservado nesse período.");
    }

    public void Cancelar(int id)
    {
        lock (_gate)
        {
            if (_reservas.RemoveAll(r => r.Id == id) != 1)
                throw new KeyNotFoundException($"Reserva {id} não encontrada.");
        }
    }

    public IReadOnlyList<Reserva> Listar()
    {
        lock (_gate)
        {
            return Array.AsReadOnly(_reservas.ToArray());
        }
    }

    public IReadOnlyList<Reserva> ListarNoPeriodo(DateTime inicio, DateTime fim, string? recurso = null)
    {
        if (fim <= inicio)
            throw new ArgumentException("O fim deve ser posterior ao início.", nameof(fim));
        if (recurso is not null && string.IsNullOrWhiteSpace(recurso))
            throw new ArgumentException("Recurso obrigatório.", nameof(recurso));

        var filtro = recurso is null ? null : Reserva.NormalizarRecurso(recurso);
        lock (_gate)
        {
            var noPeriodo = _reservas
                .Where(reserva => reserva.Conflita(inicio, fim)
                    && (filtro is null || reserva.Recurso.Equals(filtro, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(reserva => reserva.Inicio)
                .ThenBy(reserva => reserva.Id)
                .ToArray();
            return Array.AsReadOnly(noPeriodo);
        }
    }
}
