# Task Manager API

API REST para criar, listar, editar e excluir tarefas, feita com C#, .NET 10, ASP.NET Core, Entity Framework Core e SQL Server.

## Stack

- .NET 10 e ASP.NET Core (controllers)
- Entity Framework Core 10 com SQL Server
- Documentação interativa com OpenAPI e Scalar
- Testes de integração com xUnit e WebApplicationFactory

## Executando

Requer o SDK do .NET 10 e um SQL Server. A forma mais simples é o SQL Server em Docker:

```bash
docker compose up -d sqlserver
dotnet run --project src/TaskManager.Api
```

A API sobe em `http://localhost:5080` e abre a documentação interativa em `http://localhost:5080/scalar`, onde é possível testar todos os endpoints direto no navegador. O banco e a tabela são criados automaticamente na inicialização.

Para subir tudo com Docker, incluindo a API:

```bash
docker compose up --build
```

Nesse caso a API fica em `http://localhost:8080/scalar`.

Para usar outro SQL Server, como o LocalDB do Visual Studio, defina a connection string:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/TaskManager.Api
```

Testes (usam banco em memória, sem precisar de SQL Server):

```bash
dotnet test
```

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| GET | `/api/tasks` | Lista tarefas com filtros, ordenação e paginação |
| GET | `/api/tasks/{id}` | Busca uma tarefa |
| POST | `/api/tasks` | Cria uma tarefa |
| PUT | `/api/tasks/{id}` | Edita título, descrição, status, prioridade e prazo |
| PATCH | `/api/tasks/{id}/status` | Altera apenas o status |
| DELETE | `/api/tasks/{id}` | Exclui uma tarefa |

Filtros de `GET /api/tasks`: `status` (`Pending`, `InProgress`, `Completed`), `priority` (`Low`, `Medium`, `High`), `search` (título ou descrição), `dueBefore`, `dueAfter`, `overdue`, `sortBy` (`CreatedAt`, `UpdatedAt`, `DueDate`, `Priority`, `Status`, `Title`), `descending`, `page` e `pageSize` (máximo de 100).

Exemplo de criação:

```json
{
  "title": "Estudar para a prova",
  "description": "Revisar os capítulos 3 e 4",
  "priority": "High",
  "dueDate": "2026-12-01T12:00:00Z"
}
```

Regras de negócio: ao marcar uma tarefa como `Completed`, `completedAt` é preenchido; ao reabri-la, volta a ficar vazio. Tarefas com prazo vencido e não concluídas retornam `isOverdue: true`. Erros de validação e de recurso inexistente seguem o formato ProblemDetails (RFC 9457).

## Estrutura

```
src/TaskManager.Api
  Controllers   endpoints REST
  Data          DbContext e inicialização do banco
  Dtos          requests, responses e paginação
  Errors        tratamento global de exceções
  Mappers       conversão entidade para DTO
  Models        entidade e enums
  Services      regras de negócio
tests/TaskManager.Api.Tests
```

## Observações

- O esquema é criado com `EnsureCreated`. Para evoluir o banco em produção, adote migrations com `dotnet ef migrations add Initial --project src/TaskManager.Api` (o pacote `Microsoft.EntityFrameworkCore.Design` já está referenciado) e troque `EnsureCreatedAsync` por `MigrateAsync` em `DatabaseInitializer`.
- A senha do SQL Server em `appsettings.json` e no `docker-compose.yml` é apenas para desenvolvimento.
