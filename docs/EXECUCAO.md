# Execução e limitações

## Pré-requisitos
.NET SDK 8, Node.js 20+, Docker Compose e PostgreSQL.

## Build e testes
```bash
dotnet build src/ReservaSalas.Api/ReservaSalas.Api.csproj
dotnet test tests/ReservaSalas.UnitTests/ReservaSalas.UnitTests.csproj
cd frontend && npm install && npm run build
```

## Banco de dados
O modelo EF Core está implementado, mas **a migração inicial ainda precisa ser gerada e revisada** antes do primeiro uso:
```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add InitialCreate --project src/ReservaSalas.Api --startup-project src/ReservaSalas.Api
dotnet ef database update --project src/ReservaSalas.Api --startup-project src/ReservaSalas.Api
```
Defina ConnectionStrings__Default e Jwt__Key no ambiente antes dos comandos. Execute a migração antes de iniciar a API. Não use EnsureCreated em produção.

## Desenvolvimento
Copie .env.example para .env e substitua os segredos por valores aleatórios. Execute `docker compose up --build` para subir PostgreSQL e API (após a migração). No frontend, execute `npm install && npm run dev`. A API usa localhost:8080 e o frontend localhost:5173.

## Autenticação
A API **valida** JWTs assinados por chave simétrica configurada, com issuer, audience e expiração. Ela **não emite tokens nem possui login**. Configure um provedor emissor confiável para uso real; o formulário atual aceita um token de desenvolvimento fornecido externamente. A criação de salas requer role `admin`. O cancelamento exige ser o proprietário ou administrador.

## Pendências para produção
- Criar e versionar migrations e testes de integração com PostgreSQL.
- Implementar login com provedor de identidade e renovação segura de sessão.
- Testar serialização/conflitos concorrentes em PostgreSQL sob carga.
- Adicionar trilha de auditoria, observabilidade e políticas de segurança.
- Configurar HTTPS, rate limiting e políticas de implantação.
- Executar builds e testes em CI antes de liberar deploy.
