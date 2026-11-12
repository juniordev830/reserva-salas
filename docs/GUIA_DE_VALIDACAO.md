# Roteiro de manutenção — Reserva Salas

## O que o código faz

Núcleo .NET de reserva de salas e prevenção de conflitos, sem sistema web publicado.

## Arquivos de referência

`src/ReservaSalas/Domain.cs`, `tests/ReservaSalas.Tests/Program.cs` e respectivos arquivos .csproj.

## Executar verificações

`dotnet build src/ReservaSalas/ReservaSalas.csproj`; `dotnet run --project tests/ReservaSalas.Tests/ReservaSalas.Tests.csproj`.

Use ambiente local e dados fictícios. O CI complementa, mas não substitui a inspeção do comportamento e das entradas de fronteira.

## Testes que não podem regredir

Validar reservas coincidentes, adjacentes, término anterior ao início, sala inexistente e remoção de reserva em andamento.

## Próxima melhoria útil

Antes de uso coletivo, definir timezone, concorrência transacional, calendário de salas e autenticação.

### Antes de abrir PR

- [ ] Teste ou exemplo reproduzível para a alteração.
- [ ] README atualizado se o comando ou escopo mudou.
- [ ] Dados privados e segredos fora do controle de versão.
- [ ] Revisão em branch, sem merge automático.
