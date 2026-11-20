# Reserva Salas

Reserva de salas e equipamentos com controle de conflitos, agenda diária, consulta de disponibilidade e persistência local em JSON.

## Estado atual

Aplicação de domínio em C# / .NET 8. O núcleo legado mantém reservas em memória com prevenção de conflitos e cancelamento. O módulo `AgendaSalas` adiciona cadastro de recursos, agenda com horários, reagendamento, consulta de disponibilidade e snapshots JSON locais. Ainda não há API ou interface integrada.

## Funcionalidades da agenda detalhada

- Recursos com código, descrição e capacidade (1 a 500 pessoas).
- Reservas com `DateTimeOffset`, entre 08h e 20h, com duração de 15 minutos a 12 horas.
- Prevenção de conflitos, reagendamento atômico, cancelamento e filtros.
- Consulta de horários livres em intervalos de 30 minutos e resumo de ocupação.
- Persistência local em JSON versionado, com gravação atômica e limite de 1 MB.

## Tecnologias implementadas

.NET 8 e C#.

## Escopo da primeira versão

- [x] Cadastrar recursos
- [x] Reservar períodos disponíveis
- [x] Cancelar e consultar reservas
- [x] Consultar as janelas livres de um recurso a partir de uma duração mínima

## Critérios para concluir a primeira versão

- [ ] Implementar o fluxo principal com validação de entradas e tratamento de erros.
- [ ] Documentar instalação, configuração e execução com comandos reproduzíveis.
- [ ] Adicionar exemplos de uso e dados fictícios.
- [ ] Verificar os casos principais e limites e registrar os resultados.

## Desenvolvimento

Execute na raiz do repositório:

```bash
dotnet build src/ReservaSalas/ReservaSalas.csproj
dotnet run --project tests/ReservaSalas.Tests/ReservaSalas.Tests.csproj
```

O GitHub Actions executa a validação em pushes e pull requests. A persistência JSON é local e demonstrativa; não é banco multiusuário nem substitui uma API transacional.

## Privacidade e configuração

Usar dados fictícios nos exemplos e manter senhas, tokens, arquivos pessoais e credenciais fora do controle de versão.

## Licença

Ainda não definida.

## Documentação para manutenção

O [guia de validação](docs/GUIA_DE_VALIDACAO.md) contém testes de regressão, pontos críticos e critérios para revisar a próxima alteração.
