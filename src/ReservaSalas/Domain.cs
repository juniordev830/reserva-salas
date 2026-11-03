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
    private readonly object _sync = new();
    private readonly List<Reserva> _reservas = [];
    private int _nextId = 1;

    public Reserva Reservar(string recurso, DateTime inicio, DateTime fim)
    {
        lock (_sync)
        {
            var proposta = Reserva.Criar(_nextId, recurso, inicio, fim);
            if (Conflito(proposta.Recurso, proposta.Inicio, proposta.Fim))
                throw new InvalidOperationException("O recurso já está reservado nesse período.");

            _nextId = checked(_nextId + 1);
            _reservas.Add(proposta);
            return proposta;
        }
    }

    public bool Disponivel(string recurso, DateTime inicio, DateTime fim)
    {
        var proposta = Reserva.Criar(1, recurso, inicio, fim);
        lock (_sync)
            return !Conflito(proposta.Recurso, inicio, fim);
    }

    /// <summary>
    /// Remarca a reserva existente pelo ID, validando os conflitos primeiro.
    /// Uma falha nunca libera o período anterior nem altera o identificador.
    /// </summary>
    public Reserva Reagendar(int id, DateTime inicio, DateTime fim)
    {
        lock (_sync)
        {
            var index = _reservas.FindIndex(r => r.Id == id);
            if (index < 0)
                throw new KeyNotFoundException($"Reserva {id} não encontrada.");

            var anterior = _reservas[index];
            var nova = Reserva.Criar(id, anterior.Recurso, inicio, fim);
            if (Conflito(nova.Recurso, nova.Inicio, nova.Fim, exceptId: id))
                throw new InvalidOperationException("O recurso já está reservado nesse período.");

            _reservas[index] = nova;
            return nova;
        }
    }

    public void Cancelar(int id)
    {
        lock (_sync)
        {
            var index = _reservas.FindIndex(r => r.Id == id);
            if (index < 0)
                throw new KeyNotFoundException($"Reserva {id} não encontrada.");
            _reservas.RemoveAt(index);
        }
    }

    public IReadOnlyList<Reserva> Listar()
    {
        lock (_sync)
            return Array.AsReadOnly(_reservas.ToArray());
    }

    public IReadOnlyList<Reserva> ListarRecurso(string recurso)
    {
        if (string.IsNullOrWhiteSpace(recurso))
            throw new ArgumentException("Recurso obrigatório.", nameof(recurso));
        lock (_sync)
            return Array.AsReadOnly(_reservas
                .Where(r => r.Recurso.Equals(recurso.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Inicio).ThenBy(r => r.Id).ToArray());
    }

    private bool Conflito(string recurso, DateTime inicio, DateTime fim, int exceptId = 0) =>
        _reservas.Any(r => r.Id != exceptId &&
            r.Recurso.Equals(recurso, StringComparison.OrdinalIgnoreCase) &&
            r.Conflita(inicio, fim));
}
