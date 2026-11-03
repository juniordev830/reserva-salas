# Reserva Salas

Reserva de salas e equipamentos com controle de conflitos.

## Estado atual

MVP executável em branch de desenvolvimento, com testes automatizados. Reservas em memória com prevenção de conflitos e cancelamento. Ainda não há API, interface ou persistência.

## Tecnologias implementadas

.NET 8 e C#.

## Escopo da primeira versão

- [ ] Cadastrar recursos
- [ ] Reservar períodos disponíveis
- [ ] Cancelar e consultar reservas

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

O GitHub Actions executa a validação em pushes e pull requests. Este é um núcleo demonstrativo; as funcionalidades ainda marcadas como pendentes na lista acima continuam planejadas.

## Privacidade e configuração

Usar dados fictícios nos exemplos e manter senhas, tokens, arquivos pessoais e credenciais fora do controle de versão.

## Licença

Ainda não definida.
