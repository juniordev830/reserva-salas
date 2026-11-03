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

public sealed class ReservaService
{
    private readonly List<Reserva> _reservas = [];
    private int _nextId = 1;

    public Reserva Reservar(string recurso, DateTime inicio, DateTime fim)
    {
        var proposta = Reserva.Criar(_nextId, recurso, inicio, fim);
        if (_reservas.Any(r => r.Recurso.Equals(proposta.Recurso, StringComparison.OrdinalIgnoreCase)
                               && r.Conflita(proposta.Inicio, proposta.Fim)))
            throw new InvalidOperationException("O recurso já está reservado nesse período.");

        _reservas.Add(proposta);
        _nextId++;
        return proposta;
    }

    public void Cancelar(int id)
    {
        if (!_reservas.RemoveAll(r => r.Id == id).Equals(1))
            throw new KeyNotFoundException($"Reserva {id} não encontrada.");
    }

    public IReadOnlyList<Reserva> Listar() => _reservas.AsReadOnly();
}
