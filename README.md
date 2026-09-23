# Alloca

> Sistema de gestão de reservas de salas, mesas e pavilhões — pensado para bibliotecas, espaços acadêmicos e ambientes compartilhados.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)

---

## 📑 Sumário

- [Visão geral](#-visão-geral)
- [Arquitetura](#-arquitetura)
- [Estrutura do repositório](#-estrutura-do-repositório)
- [Stack tecnológica](#-stack-tecnológica)
- [Pré-requisitos](#-pré-requisitos)
- [Como rodar o projeto](#-como-rodar-o-projeto)
- [Credenciais de seed](#-credenciais-de-seed)
- [Documentação adicional](#-documentação-adicional)
- [Scripts úteis](#-scripts-úteis)
- [Convenções de código](#-convenções-de-código)

---

## 🎯 Visão geral

**Alloca** é uma plataforma completa para gerenciar reservas de recursos físicos (salas e mesas) distribuídos em pavilhões e andares. O sistema oferece:

- **Reserva self-service** para alunos/membros, com seleção visual de andares e recursos.
- **Aprovação e moderação** por gestores de pavilhão.
- **Check-in via QR Code** para garantir o uso efetivo da reserva.
- **Bloqueios administrativos** de pavilhões, salas ou mesas (manutenção, eventos).
- **Política de strikes e suspensão automática** para no-shows, com janela e limites configuráveis.
- **Internacionalização** completa (pt-BR e en-US).
- **Gestão de usuários** com papéis: `Admin`, `PavilionManager` e `Member`.

O sistema é composto por dois projetos independentes:

| Projeto | Descrição | Stack |
|---------|-----------|-------|
| [`alloca-api`](./alloca-api/) | API REST com Clean Architecture | ASP.NET 10, EF Core, Hangfire, JWT |
| [`alloca-site`](./alloca-site/) | SPA web responsiva | Angular 21, PrimeNG, Tailwind |

---

## 🏛️ Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                     Navegador / Cliente                     │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTPS + JWT
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              alloca-site (Angular 21 SPA)                   │
│   Standalone components · Signals · PrimeNG · Tailwind      │
└────────────────────────────┬────────────────────────────────┘
                             │ REST /api
                             ▼
┌─────────────────────────────────────────────────────────────┐
│            alloca-api (ASP.NET 10 — Clean Arch)             │
│  ┌──────────┐  ┌─────────────┐  ┌──────────┐  ┌──────────┐  │
│  │   API    │→ │ Application │→ │  Domain  │  │   IoC    │  │
│  └──────────┘  └─────────────┘  └──────────┘  └──────────┘  │
│                        ↑              ↓                     │
│                  ┌─────┴──────────────┴────┐                │
│                  │  Infra (EF, Hangfire)   │                │
│                  └────────────┬────────────┘                │
└───────────────────────────────┼─────────────────────────────┘
                                ▼
                        ┌───────────────┐
                        │  SQL Server   │
                        │   (Docker)    │
                        └───────────────┘
```

Detalhes em [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md).

---

## 📂 Estrutura do repositório

```
Alloca/
├── alloca-api/             # Backend ASP.NET (Clean Architecture)
│   ├── src/
│   │   ├── Alloca.API/         # Controllers, Program.cs, settings
│   │   ├── Alloca.Application/ # Serviços, DTOs, validators
│   │   ├── Alloca.Domain/      # Entidades, enums, contratos
│   │   ├── Alloca.Infra/       # EF Core, repositórios, jobs
│   │   ├── Alloca.IoC/         # DI, auth, middleware, OpenAPI
│   │   └── Alloca.CrossCutting/
│   └── README.md
├── alloca-site/            # Frontend Angular 21
│   ├── src/app/
│   │   ├── core/               # Infra transversal (auth, http, i18n, layout, feature-flags)
│   │   ├── shared/             # UI e serviços compartilhados (layout, componentes, counters)
│   │   ├── features/           # Domínios: auth, pavilions, reservations, approvals, blocks, users, dashboard, history
│   │   └── routes/             # Roteamento por papel (student.routes, manager.routes)
│   ├── public/i18n/            # pt-BR.json, en-US.json
│   └── README.md
├── docs/                   # Documentação transversal
│   ├── ARCHITECTURE.md
│   ├── API.md
│   ├── DOMAIN.md
│   ├── DEVELOPMENT.md
│   └── DEPLOYMENT.md
├── docker-compose.yml      # SQL Server 2022 para desenvolvimento
└── README.md               # Este arquivo
```

---

## 🧰 Stack tecnológica

### Backend (`alloca-api`)

- **.NET 10** com ASP.NET Core
- **Entity Framework Core 10** + SQL Server 2022
- **FluentValidation 11** para validação de DTOs
- **JWT Bearer** para autenticação
- **PBKDF2** para hashing de senhas
- **Hangfire** para jobs recorrentes (lifecycle de reservas)
- **QRCoder** para geração de QR Codes de check-in
- **Clean Architecture** em 6 camadas

### Frontend (`alloca-site`)

- **Angular 21** standalone com `provideZonelessChangeDetection`
- **Signals** (`signal`, `computed`, `input`, `output`) e novo control flow (`@if`, `@for`)
- **PrimeNG 21** + tema Aura
- **Tailwind CSS 4** (via PostCSS) + Bootstrap 5
- **ngx-translate 17** com loader HTTP
- **Chart.js** para visualizações no dashboard
- **Quill** para edição rica
- Estrutura *feature-based* com lazy loading

### Infraestrutura

- **Docker Compose** para subir SQL Server 2022 (porta `1434`)
- **Migrations automáticas** e **seed** no boot da API

---

## ✅ Pré-requisitos

| Ferramenta | Versão recomendada |
|------------|-------------------|
| .NET SDK   | 10.0+             |
| Node.js    | 20 LTS+           |
| npm        | 10+               |
| Docker     | 24+               |
| Angular CLI | 21+ (opcional, usa-se `npx ng`) |

---

## 🚀 Como rodar o projeto

### 1. Subir o banco de dados

A partir da raiz do repositório:

```bash
docker compose up -d sqlserver
```

Isso disponibiliza um SQL Server em `localhost:1434` (usuário `sa`, senha `Alloca@Dev123!`).

### 2. Rodar a API

```bash
cd alloca-api/src/Alloca.API
dotnet restore
dotnet run
```

A API sobe em `https://localhost:7112` (e `http://localhost:5112`). Migrations e seed são aplicados automaticamente.

- Documentação OpenAPI: `https://localhost:7112/openapi/v1.json`
- Dashboard Hangfire (somente em Development): `https://localhost:7112/hangfire`

### 3. Rodar o frontend

Em outro terminal:

```bash
cd alloca-site
npm install
npm start
```

A SPA fica disponível em `http://localhost:4200`.

Mais detalhes em [`docs/DEVELOPMENT.md`](./docs/DEVELOPMENT.md).

---

## 👥 Credenciais de seed

Após o primeiro boot, os seguintes usuários estarão disponíveis para teste:

| Papel               | E-mail                  | Senha        |
|---------------------|-------------------------|--------------|
| Administrador       | `admin@alloca.local`    | `Admin@123`  |
| Gestor de Pavilhão  | `gestor@alloca.local`   | `Gestor@123` |
| Aluno (Member)      | `aluno@alloca.local`    | `Aluno@123`  |

> ⚠️ Estas credenciais existem somente em ambiente de desenvolvimento. **Não use em produção.**

---

## 📚 Documentação adicional

| Documento | Conteúdo |
|-----------|----------|
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | Visão geral arquitetural, camadas, fluxos |
| [`docs/API.md`](./docs/API.md) | Referência completa dos endpoints REST |
| [`docs/DOMAIN.md`](./docs/DOMAIN.md) | Entidades, enums e regras de negócio |
| [`docs/DEVELOPMENT.md`](./docs/DEVELOPMENT.md) | Setup local, debug, ferramentas |
| [`docs/DEPLOYMENT.md`](./docs/DEPLOYMENT.md) | Variáveis de ambiente, build e deploy |
| [`alloca-api/README.md`](./alloca-api/README.md) | Documentação específica da API |
| [`alloca-site/README.md`](./alloca-site/README.md) | Documentação específica do frontend |
| [`alloca-site/docs/i18n-conventions.md`](./alloca-site/docs/i18n-conventions.md) | Convenções de internacionalização |

---

## 🛠️ Scripts úteis

### Backend

```bash
# Restaurar dependências
dotnet restore alloca-api/alloca-api.slnx

# Build
dotnet build alloca-api/alloca-api.slnx

# Rodar (com hot reload)
dotnet watch --project alloca-api/src/Alloca.API

# Adicionar nova migration
dotnet ef migrations add NomeMigration \
  --project alloca-api/src/Alloca.Infra \
  --startup-project alloca-api/src/Alloca.API
```

### Frontend

```bash
npm start          # Dev server em http://localhost:4200
npm run build      # Build de produção
npm run watch      # Build em modo watch
npm test           # Testes unitários (Karma)
npm run format     # Prettier
```

---

## 🧑‍💻 Convenções de código

- **Backend:** C# com regras padrão do .NET, nullable habilitado, async/await em todo IO.
- **Frontend:** componentes Angular standalone com **Signals**, control flow novo (`@if`, `@for`), nomes em **português** seguindo padrão `kebab-case` para arquivos. Detalhes em [`alloca-site/docs/i18n-conventions.md`](./alloca-site/docs/i18n-conventions.md).
- **i18n:** todas as strings do frontend devem usar `TranslateService` / pipe `translate`.

---

## 📄 Licença

Este projeto está sob a licença descrita em [`alloca-site/LICENSE.md`](./alloca-site/LICENSE.md).
