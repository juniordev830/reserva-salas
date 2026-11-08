namespace ReservaSalas;

public sealed record RecursoInfo(string Codigo, string Descricao, int Capacidade)
{
    public static RecursoInfo Criar(string codigo, string descricao, int capacidade)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 40 ||
            !codigo.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            throw new ArgumentException("Código de recurso inválido.");
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Trim().Length > 120)
            throw new ArgumentException("Descrição inválida.");
        if (capacidade is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(capacidade));
        return new RecursoInfo(codigo.Trim(), descricao.Trim(), capacidade);
    }
}

public sealed record ReservaDetalhada(
    int Id, string CodigoRecurso, string Responsavel,
    DateTimeOffset Inicio, DateTimeOffset Fim, int Participantes)
{
    public TimeSpan Duracao => Fim - Inicio;
    public bool Conflita(DateTimeOffset inicio, DateTimeOffset fim) =>
        inicio < Fim && fim > Inicio;
}

public sealed record ResumoAgenda(
    string Recurso, DateOnly Dia, int Quantidade, int MinutosOcupados,
    int Capacidade, int MinutosDisponiveis);

/// <summary>
/// Agenda de salas independente do núcleo legado ReservaService.
/// Todo horário é informado com deslocamento explícito (DateTimeOffset).
/// Mutação é aplicada somente após todas as validações terem passado.
/// </summary>
public sealed class AgendaSalas
{
    private readonly Dictionary<string, RecursoInfo> _recursos =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ReservaDetalhada> _reservas = [];
    private int _proximoId = 1;

    public IReadOnlyList<RecursoInfo> Recursos() =>
        _recursos.Values.OrderBy(r => r.Codigo, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<ReservaDetalhada> Reservas() => _reservas
        .OrderBy(r => r.Inicio)
        .ThenBy(r => r.CodigoRecurso, StringComparer.OrdinalIgnoreCase)
        .ThenBy(r => r.Id)
        .ToArray();

    public void CadastrarRecurso(string codigo, string descricao, int capacidade)
    {
        var recurso = RecursoInfo.Criar(codigo, descricao, capacidade);
        if (!_recursos.TryAdd(recurso.Codigo, recurso))
            throw new InvalidOperationException("Este código de recurso já está cadastrado.");
    }

    public RecursoInfo BuscarRecurso(string codigo) =>
        _recursos.GetValueOrDefault(codigo) ??
        throw new KeyNotFoundException("Recurso não cadastrado: " + codigo);

    public bool RemoverRecurso(string codigo)
    {
        if (!_recursos.ContainsKey(codigo)) return false;
        if (_reservas.Any(r => r.CodigoRecurso.Equals(codigo, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Não é possível remover um recurso com reservas.");
        return _recursos.Remove(codigo);
    }

    private static void ValidarPeriodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio)
            throw new ArgumentException("O término deve ser depois do início.");
        var duracao = fim - inicio;
        if (duracao < TimeSpan.FromMinutes(15) || duracao > TimeSpan.FromHours(12))
            throw new ArgumentOutOfRangeException(nameof(fim), "Duração: 15 minutos a 12 horas.");
        // Operação local: 08:00 a 20:00, sem atravessar a meia-noite.
        if (inicio.Offset != fim.Offset || inicio.Date != fim.Date ||
            inicio.TimeOfDay < TimeSpan.FromHours(8) ||
            fim.TimeOfDay > TimeSpan.FromHours(20))
            throw new ArgumentException("Reservas devem ficar no mesmo dia, entre 08:00 e 20:00, no mesmo fuso.");
    }

    private void ValidarReserva(
        string codigo, string responsavel, DateTimeOffset inicio,
        DateTimeOffset fim, int participantes, int? idExcluido = null)
    {
        var recurso = BuscarRecurso(codigo);
        if (string.IsNullOrWhiteSpace(responsavel) ||
            responsavel.Trim().Length > 100 ||
            responsavel.Any(char.IsControl))
            throw new ArgumentException("Responsável inválido.");
        if (participantes < 1 || participantes > recurso.Capacidade)
            throw new ArgumentOutOfRangeException(nameof(participantes),
                "Participantes excedem a capacidade do recurso.");
        ValidarPeriodo(inicio, fim);
        if (_reservas.Any(r =>
            r.Id != idExcluido &&
            r.CodigoRecurso.Equals(recurso.Codigo, StringComparison.OrdinalIgnoreCase) &&
            r.Conflita(inicio, fim)))
            throw new InvalidOperationException("Conflito de horário neste recurso.");
    }

    public ReservaDetalhada Reservar(
        string codigo, string responsavel, DateTimeOffset inicio,
        DateTimeOffset fim, int participantes)
    {
        ValidarReserva(codigo, responsavel, inicio, fim, participantes);
        var booking = new ReservaDetalhada(_proximoId++, BuscarRecurso(codigo).Codigo,
            responsavel.Trim(), inicio, fim, participantes);
        _reservas.Add(booking);
        return booking;
    }

    public ReservaDetalhada Reagendar(
        int id, DateTimeOffset novoInicio, DateTimeOffset novoFim)
    {
        var atual = _reservas.FirstOrDefault(r => r.Id == id) ??
            throw new KeyNotFoundException("Reserva não encontrada.");
        ValidarReserva(atual.CodigoRecurso, atual.Responsavel,
            novoInicio, novoFim, atual.Participantes, id);
        var atualizado = atual with { Inicio = novoInicio, Fim = novoFim };
        var index = _reservas.FindIndex(r => r.Id == id);
        _reservas[index] = atualizado;
        return atualizado;
    }

    public void Cancelar(int id)
    {
        if (_reservas.RemoveAll(r => r.Id == id) != 1)
            throw new KeyNotFoundException("Reserva não encontrada: " + id);
    }

    public IReadOnlyList<ReservaDetalhada> Consultar(
        DateOnly? dia = null, string? recurso = null, string? responsavel = null)
    {
        IEnumerable<ReservaDetalhada> resultado = Reservas();
        if (dia is not null)
            resultado = resultado.Where(r =>
                DateOnly.FromDateTime(r.Inicio.Date) == dia.Value);
        if (!string.IsNullOrWhiteSpace(recurso))
            resultado = resultado.Where(r =>
                r.CodigoRecurso.Equals(recurso, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(responsavel))
            resultado = resultado.Where(r =>
                r.Responsavel.Contains(responsavel, StringComparison.OrdinalIgnoreCase));
        return resultado.ToArray();
    }

    public IReadOnlyList<DateTimeOffset> HorariosLivres(
        string codigo, DateOnly dia, TimeSpan duracao, TimeSpan deslocamentoFuso)
    {
        BuscarRecurso(codigo);
        if (duracao < TimeSpan.FromMinutes(15) || duracao > TimeSpan.FromHours(12))
            throw new ArgumentOutOfRangeException(nameof(duracao));
        if (deslocamentoFuso < TimeSpan.FromHours(-14) ||
            deslocamentoFuso > TimeSpan.FromHours(14) ||
            deslocamentoFuso.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new ArgumentOutOfRangeException(nameof(deslocamentoFuso));
        var inicio = new DateTimeOffset(dia.ToDateTime(new TimeOnly(8, 0)),
            deslocamentoFuso);
        var limite = new DateTimeOffset(dia.ToDateTime(new TimeOnly(20, 0)),
            deslocamentoFuso);
        var horarios = new List<DateTimeOffset>();
        for (var cursor = inicio; cursor + duracao <= limite;
             cursor = cursor.AddMinutes(30))
        {
            if (!_reservas.Any(r =>
                r.CodigoRecurso.Equals(codigo, StringComparison.OrdinalIgnoreCase) &&
                r.Conflita(cursor, cursor + duracao)))
                horarios.Add(cursor);
        }
        return horarios;
    }

    public ResumoAgenda Resumo(string codigo, DateOnly dia)
    {
        var recurso = BuscarRecurso(codigo);
        var marcadas = Consultar(dia, codigo);
        int minutos = marcadas.Sum(r => (int)r.Duracao.TotalMinutes);
        return new ResumoAgenda(
            recurso.Codigo, dia, marcadas.Count, minutos,
            recurso.Capacidade, 12 * 60 - minutos);
    }

    // Used only by controlled local snapshot loader; retains original identifiers.
    public static AgendaSalas Restaurar(
        IEnumerable<RecursoInfo> recursos, IEnumerable<ReservaDetalhada> reservas)
    {
        var agenda = new AgendaSalas();
        foreach (var recurso in recursos)
            agenda.CadastrarRecurso(recurso.Codigo, recurso.Descricao, recurso.Capacidade);
        foreach (var r in reservas.OrderBy(x => x.Id))
        {
            if (r.Id < 1 || r.Id < agenda._proximoId)
                throw new InvalidDataException("Identificadores de reservas inválidos.");
            agenda.ValidarReserva(r.CodigoRecurso, r.Responsavel,
                r.Inicio, r.Fim, r.Participantes);
            var normalizado = r with {
                CodigoRecurso = agenda.BuscarRecurso(r.CodigoRecurso).Codigo,
                Responsavel = r.Responsavel.Trim(),
            };
            agenda._reservas.Add(normalizado);
            agenda._proximoId = checked(r.Id + 1);
        }
        return agenda;
    }
}
