# Reserva Salas

Biblioteca em **C# / .NET 8** para reservar salas e equipamentos, consultar disponibilidade e remarcar horários sem perder a reserva original.

## Funcionalidades

- `Reservar(recurso, inicio, fim)`: cria um ID único, rejeitando sobreposição para o mesmo recurso.
- `Disponivel(recurso, inicio, fim)`: consulta de disponibilidade sem modificar a agenda.
- `Reagendar(id, inicio, fim)`: remarca a reserva **mantendo o mesmo ID**, após validar o intervalo e conflitos com outras reservas. Uma falha preserva o horário original.
- `Cancelar(id)`: remove exatamente a reserva solicitada; ID desconhecido gera exceção.
- `Listar()`: retorna snapshot não modificável de todas as reservas.
- `ListarRecurso(recurso)`: filtra sem diferenciar maiúsculas/minúsculas e ordena por início e ID.

A convenção para intervalos é **[início, fim)**; reservas adjacentes não são conflitantes. Operações são protegidas por um lock para impedir reservas duplicadas em threads da mesma instância.

## Exemplo

```csharp
using ReservaSalas;

var agenda = new ReservaService();
var inicio = new DateTime(2026, 11, 3, 9, 0, 0);
var reserva = agenda.Reservar("Sala A", inicio, inicio.AddHours(1));
Console.WriteLine(agenda.Disponivel("sala a", inicio, inicio.AddHours(1))); // false
var atualizada = agenda.Reagendar(reserva.Id, inicio.AddHours(2), inicio.AddHours(3));
Console.WriteLine(agenda.ListarRecurso("Sala A").Count); // 1
agenda.Cancelar(atualizada.Id);
```

## Compilar e testar

```bash
dotnet build src/ReservaSalas/ReservaSalas.csproj --configuration Release
dotnet run --project tests/ReservaSalas.Tests/ReservaSalas.Tests.csproj --configuration Release
```

O GitHub Actions executa os smoke tests com cobertura de conflito, remarcação, rollback, filtro, snapshots e reservas simultâneas.

## Limitações

Núcleo em memória sem API, autenticação, armazenamento persistente, calendário ou coordenação entre processos. Usa `DateTime` diretamente; o fuso e a conversão de horários devem ser definidos na futura aplicação. IDs são locais à instância e não são identificadores persistentes.

Os dados do exemplo são fictícios. Licença ainda não definida.
