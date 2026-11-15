# Regras de reserva e critérios de aceite

Este documento descreve comportamentos esperados para a evolução do MVP. Não declara que funcionalidades futuras já estejam implementadas.

## Regras de domínio

1. Uma reserva deve identificar o recurso, o instante inicial e o instante final.
2. O instante final deve ser estritamente posterior ao inicial.
3. Reservas ativas do mesmo recurso não podem ter períodos sobrepostos.
4. Intervalos adjacentes são permitidos: uma reserva que termina às 10:00 não bloqueia outra que começa às 10:00.
5. Reservas de recursos diferentes podem ocorrer no mesmo horário.
6. Uma reserva cancelada deixa de bloquear o recurso; o histórico do cancelamento deve ser preservado quando houver persistência.
7. Operações inválidas devem retornar um erro claro, sem alterar reservas existentes.

## Definição de sobreposição

Para intervalos semiabertos `[inicio, fim)`, duas reservas `A` e `B` conflitam quando `A.inicio < B.fim` **e** `B.inicio < A.fim`, desde que estejam ativas e pertençam ao mesmo recurso.

## Cenários de regressão

| Caso | Preparação | Resultado esperado |
| --- | --- | --- |
| Conflito total | Reservar sala A das 09:00 às 11:00; tentar 09:30–10:30 | Segunda reserva rejeitada |
| Conflito parcial | Reservar sala A das 09:00 às 11:00; tentar 10:30–12:00 | Segunda reserva rejeitada |
| Limite adjacente | Reservar sala A das 09:00 às 10:00; tentar 10:00–11:00 | Segunda reserva aceita |
| Recurso distinto | Reservar sala A e sala B das 09:00 às 10:00 | Ambas aceitas |
| Período vazio | Tentar 10:00–10:00 | Rejeitado |
| Período invertido | Tentar 11:00–10:00 | Rejeitado |
| Cancelamento | Reservar, cancelar e reservar o mesmo período | Nova reserva aceita |
| Consulta | Criar duas reservas válidas e consultar | Ambas retornadas conforme contrato |

## Evolução para API e persistência

- Usar datas com fuso explícito ou normalizadas em UTC; converter para horário local apenas na apresentação.
- Validar entradas na borda da API e novamente nas regras de domínio.
- Prevenir corridas entre duas reservas simultâneas por transação/controle de concorrência no armazenamento, não apenas por consulta prévia.
- Proteger operações de cancelamento com autorização e registrar auditoria sem dados sensíveis.
- Criar testes automatizados para cada cenário acima antes de publicar uma API.

## Como validar o MVP atual

```bash
dotnet build src/ReservaSalas/ReservaSalas.csproj
dotnet run --project tests/ReservaSalas.Tests/ReservaSalas.Tests.csproj
```

A execução desses comandos deve ser confirmada em CI ou ambiente com .NET 8; a existência deste guia, por si só, não comprova que os testes passaram.
