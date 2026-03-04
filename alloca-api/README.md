# Alloca.API

Backend REST do **Alloca**, construído em **ASP.NET Core 10** seguindo **Clean Architecture**.

> Para visão geral do produto, consulte o [README raiz](../README.md). Para a referência completa de endpoints, consulte [`docs/API.md`](../docs/API.md).

---

## 📑 Sumário

- [Estrutura da solução](#estrutura-da-solução)
- [Stack](#stack)
- [Como rodar](#como-rodar)
- [Configuração](#configuração)
- [Migrations e seed](#migrations-e-seed)
- [Autenticação](#autenticação)
- [Background jobs](#background-jobs)
- [Testes](#testes)

---

## Estrutura da solução

```
alloca-api/
├── alloca-api.slnx
└── src/
    ├── Alloca.API/             # Presentation: Controllers, Program.cs, settings
    │   ├── Controllers/
    │   │   ├── AuthController.cs
    │   │   ├── ManagerController.cs
    │   │   ├── PavilionsController.cs
    │   │   ├── ReservationsController.cs
    │   │   └── UsersController.cs
    │   ├── Properties/launchSettings.json
    │   ├── appsettings.json
    │   ├── seed-data.json
    │   └── Program.cs
    │
    ├── Alloca.Application/     # Application: casos de uso, DTOs, validators
    │   ├── Common/
    │   │   ├── Exceptions/
    │   │   ├── Interfaces/
    │   │   └── Settings/
    │   ├── DTOs/
    │   │   ├── Auth/  Availability/  Blocks/
    │   │   ├── Manager/  Pavilions/  Reservations/  Users/
    │   ├── Services/           # Interfaces (IAuthService, etc.) + implementações
    │   ├── Validators/         # FluentValidation
    │   └── DependencyInjection.cs
    │
    ├── Alloca.Domain/          # Núcleo: entidades, enums, VOs, contratos
    │   ├── Common/             # Entity (base)
    │   ├── Entities/
    │   ├── Enums/
    │   ├── ValueObjects/       # TimeRange
    │   └── Repositories/       # Interfaces
    │
    ├── Alloca.Infra/           # Implementações: EF Core, JWT, hashing, jobs
    │   ├── Background/         # ReservationLifecycleJob (Hangfire)
    │   ├── Persistence/        # DbContext, Configurations, Repositories, Seed
    │   ├── Services/           # JwtTokenService, PasswordHasher, QrCodeService
    │   └── DependencyInjection.cs
    │
    ├── Alloca.IoC/             # Composition root: DI, middlewares, OpenAPI, auth
    │   ├── Auth/
    │   ├── Config/             # CorsConfig, ExceptionHandlingConfig, LocalizationConfig
    │   ├── Middleware/
    │   ├── OpenApi/
    │   └── NativeInjectorBootstrapper.cs
    │
    └── Alloca.CrossCutting/    # Utilitários neutros
```

A regra de dependência aponta sempre para o **`Alloca.Domain`**. Detalhes em [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md).

---

## Stack

| Recurso          | Tecnologia                                       |
|------------------|--------------------------------------------------|
| Runtime          | .NET 10 / ASP.NET Core                           |
| ORM              | Entity Framework Core 10 + SQL Server            |
| Validação        | FluentValidation 11                              |
| Autenticação     | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Hashing          | PBKDF2 (`Microsoft.AspNetCore.Cryptography.KeyDerivation`) |
| Background jobs  | Hangfire 1.8 + Hangfire.SqlServer                |
| QR Code          | QRCoder 1.6                                      |
| Documentação     | OpenAPI (built-in do .NET 10)                    |

---

## Como rodar

### 1. Banco de dados

A partir da raiz do repositório:

```bash
docker compose up -d sqlserver
```

> Sobe SQL Server 2022 em `localhost:1434` (usuário `sa`, senha `Alloca@Dev123!`).

### 2. API

```bash
cd src/Alloca.API
dotnet run
```

- HTTPS: `https://localhost:7112`
- HTTP: `http://localhost:5112`
- OpenAPI: `https://localhost:7112/openapi/v1.json`
- Hangfire Dashboard (apenas Development): `https://localhost:7112/hangfire`

Para hot reload:

```bash
dotnet watch --project src/Alloca.API
```

---

## Configuração

`appsettings.json` (valores default de dev):

```json
{
  "ConnectionStrings": {
    "AllocaDb": "Server=localhost,1434;Database=AllocaDb;User Id=sa;Password=Alloca@Dev123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "Alloca",
    "Audience": "Alloca",
    "Secret": "CHANGE-ME-DEV-ONLY-PLEASE-USE-A-LONG-RANDOM-SECRET-32B+",
    "ExpirationMinutes": 480
  },
  "ReservationPolicy": {
    "MinDurationMinutes": 30,
    "MaxDurationMinutes": 120,
    "MaxActiveReservations": 2,
    "CancellationCutoffHours": 2,
    "NoShowGraceMinutes": 15,
    "StrikeWindowDays": 30,
    "StrikeSuspensionThreshold": 3,
    "StrikeSuspensionDays": 7
  }
}
```

Em produção, sobrescreva via variáveis de ambiente (formato `Secao__Chave`). Detalhes em [`docs/DEPLOYMENT.md`](../docs/DEPLOYMENT.md).

---

## Migrations e seed

As migrations são aplicadas **automaticamente** no boot:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AllocaDbContext>();
    db.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}
```

O **seed** lê `src/Alloca.API/seed-data.json` e cria:

- 3 usuários de exemplo (admin, gestor, aluno) — ver [README raiz](../README.md#-credenciais-de-seed).
- 1 pavilhão `BCP` (Biblioteca Central) com:
  - Horários de funcionamento (seg-sex 8–20h, sáb 8–17h)
  - 1 andar (`BCP01`)
  - Várias salas/mesas

### Criar nova migration

```bash
dotnet ef migrations add NomeMigration \
  --project src/Alloca.Infra \
  --startup-project src/Alloca.API \
  --output-dir Persistence/Migrations
```

### Aplicar migrations manualmente

```bash
dotnet ef database update \
  --project src/Alloca.Infra \
  --startup-project src/Alloca.API
```

---

## Autenticação

Fluxo JWT padrão:

1. Cliente faz `POST /api/auth/login` com `email`/`password`.
2. API valida, gera JWT com claim `role` e retorna em `LoginResponse`.
3. Cliente envia em todas as chamadas seguintes:

   ```http
   Authorization: Bearer <token>
   ```

Controllers usam `[Authorize]` para autenticação simples e `[Authorize(Roles = "PavilionManager,Admin")]` para autorização por papel.

> O `Jwt:Secret` precisa ter pelo menos 32 bytes para passar nas validações do `JwtBearerOptions`.

---

## Background jobs

`Alloca.Infra/Background/ReservationLifecycleJob.cs` é executado **a cada minuto** via Hangfire:

```csharp
RecurringJob.AddOrUpdate<ReservationLifecycleJob>(
    "reservation-lifecycle", j => j.RunAsync(), "* * * * *");
```

Responsabilidades:
1. Marcar `Approved` como `NoShow` após `NoShowGraceMinutes`.
2. Emitir `UserStrike` por no-show.
3. Criar `UserSuspension` quando atingir `StrikeSuspensionThreshold`.
4. Transitar `InProgress` → `Completed` após `EndUtc`.

Tabelas do Hangfire são criadas automaticamente no schema `HangFire`. O dashboard fica em `/hangfire` (apenas Development).

---

## Testes

> Projeto de testes ainda não incluído nesta versão. Estrutura recomendada:
>
> ```
> tests/
> ├── Alloca.Domain.Tests/
> ├── Alloca.Application.Tests/
> └── Alloca.API.IntegrationTests/
> ```
>
> Sugestões: **xUnit**, **FluentAssertions**, **Testcontainers** (SQL Server real) e **WebApplicationFactory** para testes de integração.
