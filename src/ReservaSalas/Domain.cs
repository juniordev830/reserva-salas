namespace ReservaSalas;

public sealed record Reserva(int Id, string Recurso, DateTime Inicio, DateTime Fim)
{
    public static Reserva Criar(int id, string recurso, DateTime inicio, DateTime fim)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(recurso)) throw new ArgumentException("Recurso obrigatório.", nameof(recurso));
        if (fim <= inicio) throw new ArgumentException("O fim deve ser posterior ao início.", nameof(fim));
        return new Reserva(id, recurso.Trim(), inicio, fim);
    }

    public bool Conflita(DateTime inicio, DateTime fim) =>
        inicio < Fim && fim > Inicio;
}

/// <summary>
/// Implementação em memória para demonstrações e testes locais.
/// A API persistente utiliza BookingDb e transações PostgreSQL.
/// </summary>
public sealed class ReservaService
{
    private readonly object _sync = new();
    private readonly List<Reserva> _reservas = [];
    private int _nextId = 1;

    public Reserva Reservar(string recurso, DateTime inicio, DateTime fim)
    {
        lock (_sync)
        {
            var proposta = Reserva.Criar(_nextId, recurso, inicio, fim);
            if (_reservas.Any(r => r.Recurso.Equals(proposta.Recurso, StringComparison.OrdinalIgnoreCase)
                                   && r.Conflita(proposta.Inicio, proposta.Fim)))
                throw new InvalidOperationException("O recurso já está reservado nesse período.");

            _reservas.Add(proposta);
            _nextId++;
            return proposta;
        }
    }

    public void Cancelar(int id)
    {
        lock (_sync)
        {
            if (_reservas.RemoveAll(r => r.Id == id) == 0)
                throw new KeyNotFoundException($"Reserva {id} não encontrada.");
        }
    }

    public IReadOnlyList<Reserva> Listar()
    {
        lock (_sync)
            return _reservas.ToArray();
    }
}
