# Reserva Salas

Sistema de reservas em **C# / .NET 8**, com cadastro de salas e equipamentos, prevenção de conflitos, pesquisa de disponibilidade e persistência local em JSON.

## Funcionalidades da nova branch

- Cadastro de recursos por código, descrição e capacidade (1 a 500 pessoas).
- Validação de identificadores, dados de responsáveis e quantidade de participantes.
- Reservas com início e fim usando `DateTimeOffset` para preservar o fuso horário.
- Agenda local diária das **08h às 20h**, com duração de 15 minutos até 12 horas.
- Prevenção de reservas sobrepostas, inclusive entre nomes de recursos com maiúsculas diferentes.
- Reagendamento com manutenção do identificador original e validação antes de alterar o estado.
- Cancelamento, filtro por dia, recurso e responsável.
- Consulta de horários livres em intervalos de 30 minutos.
- Resumo de capacidade e minutos ocupados/disponíveis.
- Salvamento de recursos e reservas em JSON local versionado e gravação por substituição atômica.
- Testes sem dependências externas, executados como aplicação de console.

O serviço original `ReservaService` continua presente e com o comportamento original preservado.

## Testar

Na raiz do repositório:

```bash
dotnet build src/ReservaSalas/ReservaSalas.csproj --configuration Release
dotnet run --project tests/ReservaSalas.Tests/ReservaSalas.Tests.csproj --configuration Release
```

O GitHub Actions compila o projeto e executa a suíte de regressão em pushes e pull requests.

## Exemplo de uso

```csharp
using ReservaSalas;

var agenda = new AgendaSalas();
agenda.CadastrarRecurso("sala-a", "Sala de treinamento", 12);

var fuso = TimeSpan.FromHours(-3);
var inicio = new DateTimeOffset(2026, 11, 9, 9, 0, 0, fuso);
var fim = inicio.AddHours(1);

var reserva = agenda.Reservar("sala-a", "Ana", inicio, fim, 8);
var livres = agenda.HorariosLivres(
    "sala-a", new DateOnly(2026, 11, 9),
    TimeSpan.FromHours(1), fuso);

var resumo = agenda.Resumo("sala-a", new DateOnly(2026, 11, 9));
ArquivoAgenda.Salvar(agenda, "agenda-demo.json");
var restaurada = ArquivoAgenda.Carregar("agenda-demo.json");
```

## Limitações

Este é um **núcleo de biblioteca com armazenamento local**, não um sistema de reservas multiusuário. Não há autenticação, API HTTP, interface gráfica ou controle de escrita concorrente de múltiplos processos. O exemplo e os testes usam dados fictícios.

**Privacidade:** o arquivo JSON não é criptografado; mantenha dados reais de responsáveis e agendas em ambiente privado e não envie o arquivo ao GitHub.

## Licença

Ainda não definida.
