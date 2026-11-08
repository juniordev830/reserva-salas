using System.Text.Json;
using ReservaSalas;

public static class AgendaSalasTests
{
    private static int _checks;
    private static readonly TimeSpan Fuso = TimeSpan.FromHours(-3);
    private static readonly DateOnly Dia = new(2026, 11, 7);

    private static DateTimeOffset Hora(int hora, int minuto = 0) =>
        new(2026, 11, 7, hora, minuto, 0, Fuso);

    private static void Verificar(bool condicao, string mensagem)
    {
        _checks++;
        if (!condicao) throw new Exception("Falha: " + mensagem);
    }

    private static T DeveFalhar<T>(Action acao) where T : Exception
    {
        _checks++;
        try
        {
            acao();
        }
        catch (T error)
        {
            return error;
        }
        catch (Exception error)
        {
            throw new Exception("Era esperada " + typeof(T).Name +
                ", mas recebemos " + error.GetType().Name, error);
        }
        throw new Exception("Era esperada exceção " + typeof(T).Name);
    }

    private static AgendaSalas NovaAgenda()
    {
        var agenda = new AgendaSalas();
        agenda.CadastrarRecurso("sala-A", "Sala de reunião 1", 8);
        agenda.CadastrarRecurso("projetor", "Projetor de apresentação", 1);
        return agenda;
    }

    private static void CadastroDeRecursos()
    {
        var agenda = NovaAgenda();
        Verificar(agenda.Recursos().Count == 2, "dois recursos cadastrados");
        Verificar(agenda.BuscarRecurso("SALA-a").Capacidade == 8,
            "busca independente de maiúsculas");
        DeveFalhar<InvalidOperationException>(() =>
            agenda.CadastrarRecurso("SALA-A", "duplicada", 4));
        DeveFalhar<ArgumentException>(() =>
            agenda.CadastrarRecurso("espaço inválido", "Recurso", 4));
        DeveFalhar<ArgumentException>(() =>
            agenda.CadastrarRecurso("ok", "", 4));
        DeveFalhar<ArgumentOutOfRangeException>(() =>
            agenda.CadastrarRecurso("ok", "Sala", 0));
        DeveFalhar<KeyNotFoundException>(() => agenda.BuscarRecurso("desconhecido"));
        Verificar(agenda.RemoverRecurso("projetor"), "remoção do projetor livre");
        Verificar(!agenda.RemoverRecurso("projetor"), "remoção repetida é falsa");
    }

    private static void CriacaoEConflitos()
    {
        var agenda = NovaAgenda();
        var inicial = agenda.Reservar("Sala-A", "Ana", Hora(9), Hora(10), 4);
        Verificar(inicial.Id == 1, "primeiro identificador");
        Verificar(inicial.Duracao == TimeSpan.FromHours(1), "duração correta");

        DeveFalhar<InvalidOperationException>(() =>
            agenda.Reservar("sala-a", "Bruno", Hora(9, 30), Hora(10, 30), 2));
        var adjacente = agenda.Reservar("sala-a", "Bruno", Hora(10), Hora(11), 2);
        Verificar(adjacente.Id == 2, "rejeição de conflito não consome IDs");
        agenda.Reservar("projetor", "Carol", Hora(9), Hora(10), 1);
        Verificar(agenda.Reservas().Count == 3, "recursos diferentes coexistem");
        DeveFalhar<ArgumentOutOfRangeException>(() =>
            agenda.Reservar("sala-a", "Ana", Hora(12), Hora(13), 9));
        DeveFalhar<ArgumentException>(() =>
            agenda.Reservar("sala-a", "   ", Hora(12), Hora(13), 1));
        DeveFalhar<ArgumentOutOfRangeException>(() =>
            agenda.Reservar("sala-a", "Ana", Hora(12), Hora(12, 10), 2));
        DeveFalhar<ArgumentException>(() =>
            agenda.Reservar("sala-a", "Ana", Hora(7), Hora(9), 2));
        DeveFalhar<ArgumentException>(() =>
            agenda.Reservar("sala-a", "Ana", Hora(19), Hora(21), 2));
        DeveFalhar<ArgumentException>(() =>
            agenda.Reservar("sala-a", "Ana", Hora(12), Hora(11), 2));
    }

    private static void ReagendamentoAtomico()
    {
        var agenda = NovaAgenda();
        var primeira = agenda.Reservar("sala-A", "Ana", Hora(9), Hora(10), 2);
        agenda.Reservar("sala-A", "Bruno", Hora(10), Hora(11), 2);
        DeveFalhar<InvalidOperationException>(() =>
            agenda.Reagendar(primeira.Id, Hora(10, 30), Hora(11, 30)));
        Verificar(agenda.Reservas().First().Inicio == Hora(9),
            "conflito não altera a reserva original");
        var modificada = agenda.Reagendar(primeira.Id, Hora(11), Hora(12));
        Verificar(modificada.Id == primeira.Id, "reagendamento mantém ID");
        Verificar(modificada.Responsavel == "Ana", "responsável preservado");
        Verificar(agenda.Reservas().Count == 2, "não duplica na atualização");
        DeveFalhar<KeyNotFoundException>(() => agenda.Reagendar(999, Hora(12), Hora(13)));
        DeveFalhar<InvalidOperationException>(() => agenda.RemoverRecurso("sala-a"));
        agenda.Cancelar(primeira.Id);
        DeveFalhar<KeyNotFoundException>(() => agenda.Cancelar(primeira.Id));
        Verificar(agenda.Reservas().Count == 1, "cancelamento selecionado");
    }

    private static void ConsultasEHorariosLivres()
    {
        var agenda = NovaAgenda();
        agenda.Reservar("sala-a", "Ana Silva", Hora(9), Hora(10), 3);
        agenda.Reservar("sala-a", "Bruno", Hora(13), Hora(14), 4);
        agenda.Reservar("projetor", "Ana Silva", Hora(9), Hora(10), 1);

        Verificar(agenda.Consultar(Dia).Count == 3, "consulta por dia");
        Verificar(agenda.Consultar(Dia, "SALA-A").Count == 2, "filtro por recurso");
        Verificar(agenda.Consultar(responsavel: "ANA").Count == 2,
            "busca de responsável sem case");
        Verificar(agenda.Consultar(new DateOnly(2026, 11, 8)).Count == 0,
            "dia sem reservas");
        var livres = agenda.HorariosLivres("sala-a", Dia,
            TimeSpan.FromHours(1), Fuso);
        Verificar(livres.Contains(Hora(8)), "08h é válido antes da primeira reserva");
        Verificar(!livres.Contains(Hora(9)), "09h está reservado");
        Verificar(!livres.Contains(Hora(9, 30)), "09h30 conflita com reserva");
        Verificar(livres.Contains(Hora(10)), "10h é adjacente");
        Verificar(!livres.Contains(Hora(13)), "13h está reservado");
        Verificar(livres.Contains(Hora(19)), "19h é último slot de uma hora");
        Verificar(!livres.Contains(Hora(19, 30)), "19h30 ultrapassa 20h");
        DeveFalhar<ArgumentOutOfRangeException>(() =>
            agenda.HorariosLivres("sala-a", Dia, TimeSpan.FromMinutes(10), Fuso));
        var resumo = agenda.Resumo("sala-a", Dia);
        Verificar(resumo.Quantidade == 2, "contagem de reservas");
        Verificar(resumo.MinutosOcupados == 120, "tempo ocupado");
        Verificar(resumo.MinutosDisponiveis == 600, "tempo disponível");
        Verificar(resumo.Capacidade == 8, "capacidade no resumo");
    }

    private static void PersistenciaLocal()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "gx-agenda-" +
            Guid.NewGuid().ToString("N"));
        var destino = Path.Combine(pasta, "dados.json");
        try
        {
            var vazia = ArquivoAgenda.Carregar(destino);
            Verificar(vazia.Reservas().Count == 0, "arquivo novo é agenda vazia");
            var agenda = NovaAgenda();
            agenda.Reservar("sala-a", "Ana", Hora(9), Hora(10), 2);
            agenda.Reservar("projetor", "Carol", Hora(10), Hora(11), 1);
            ArquivoAgenda.Salvar(agenda, destino);
            Verificar(File.Exists(destino), "arquivo persistido");
            var recarregada = ArquivoAgenda.Carregar(destino);
            Verificar(recarregada.Recursos().Count == 2, "recursos persistidos");
            Verificar(recarregada.Reservas().Count == 2, "reservas persistidas");
            Verificar(recarregada.Reservas()[0].Responsavel == "Ana",
                "responsável persistido");
            Verificar(recarregada.Reservar("sala-a", "Davi", Hora(11), Hora(12), 2).Id == 3,
                "sequência restaurada sem colisões de ID");

            // Invalid JSON must not be mistaken for an empty agenda.
            File.WriteAllText(destino, "{INVALIDO");
            DeveFalhar<InvalidDataException>(() => ArquivoAgenda.Carregar(destino));
            File.WriteAllText(destino, "{\"Versao\":2,\"Recursos\":[],\"Reservas\":[]}");
            DeveFalhar<InvalidDataException>(() => ArquivoAgenda.Carregar(destino));
        }
        finally
        {
            if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true);
        }
    }

    public static void Executar()
    {
        CadastroDeRecursos();
        CriacaoEConflitos();
        ReagendamentoAtomico();
        ConsultasEHorariosLivres();
        PersistenciaLocal();
        Console.WriteLine($"AgendaSalas: {_checks} verificações aprovadas.");
    }
}
