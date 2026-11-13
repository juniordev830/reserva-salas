# Plano de evolução profissional — Reserva Salas

## Diagnóstico (13/11/2026)
O repositório contém um domínio em C#/.NET 8, com reservas em memória, verificação de sobreposição, cancelamento e listagem. Há testes básicos em um executável de console e workflow de CI. Ainda não existem API, persistência, autenticação ou interface.

## Objetivo
Evoluir incrementalmente para uma aplicação web de gestão de salas e equipamentos, preservando o comportamento atual e evitando mudanças desnecessárias na branch principal.

## Etapa 1 — Qualidade do domínio
- Migrar os testes de console para xUnit, mantendo os cenários existentes.
- Testar identificadores, recursos vazios, períodos inválidos, limites adjacentes, sobreposições e cancelamentos inexistentes.
- Simplificar a condição de cancelamento e introduzir contratos claros para resultados/erros.
- Definir tratamento de UTC, conversão para fuso local e regras para horários passados.
- Formalizar o cadastro de recursos e a regra de que só recursos ativos podem ser reservados.
- Critério de aceite: build e testes automatizados verdes em CI.

## Etapa 2 — API e persistência
- Criar ASP.NET Core Web API com endpoints para recursos, disponibilidade, reservas e cancelamentos.
- Introduzir PostgreSQL e Entity Framework Core com migrations versionadas.
- Garantir prevenção de reservas simultâneas por transação e estratégia de concorrência no banco.
- Adicionar validação, paginação, filtros, OpenAPI e tratamento uniforme de erros.
- Critério de aceite: testes de integração cobrindo persistência, conflitos e erros HTTP.

## Etapa 3 — Experiência web
- Criar frontend React + TypeScript com layout responsivo, calendário e filtros.
- Implementar criação/cancelamento de reservas e estados de carregamento/erro.
- Aplicar acessibilidade básica (teclado, contraste, rótulos e feedback).
- Critério de aceite: fluxo completo de reserva demonstrável no navegador.

## Etapa 4 — Segurança
- Autenticação e autorização por perfis (administrador e usuário).
- Registro de auditoria e política de proteção de dados.
- Segredos fora do Git, configuração por ambiente e limitação de requisições.
- Critério de aceite: testes de autorização e operações auditáveis.

## Etapa 5 — Operação e documentação
- Docker Compose para desenvolvimento local, CI com testes unitários e integração.
- README com setup reproduzível, exemplos fictícios, arquitetura e decisões técnicas.
- Monitoramento básico, logs estruturados e guia de implantação.
- Critério de aceite: execução reproduzível e documentação revisada.

## Convenções de trabalho
- Implementar cada etapa em branch específica, com PR para revisão.
- Não realizar merge na main sem autorização expressa.
- Commits devem corresponder a melhorias verificáveis; evitar contribuições artificiais.
- Preservar o código existente sempre que possível.
