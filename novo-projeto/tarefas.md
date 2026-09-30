# Alloca — Backlog de Tarefas (por Épico)

> Backlog organizado em épicos para importação no Trello. Cada épico vira uma
> **lista** (coluna) ou um **rótulo**; cada tarefa vira um **cartão**.
> Sugestão de rótulos: `backend`, `frontend`, `infra`, `docs`.
> Estimativa por tamanho relativo: `P` (pequeno), `M` (médio), `G` (grande).

---

## Épico 0 — Fundação e Infraestrutura

- [ ] **[infra][M]** Criar solução .NET.
- [ ] **[infra][P]** Configurar Docker com SQL Server.
- [ ] **[infra][P]** Configurar `appsettings` (connection string, JWT, política de reserva).
- [ ] **[backend][P]** Configurar EF Core `DbContext` e `UnitOfWork`.
- [ ] **[backend][P]** Aplicar migrations e seed automáticos no boot da API.
- [ ] **[backend][P]** Middleware global de tratamento de erros (ProblemDetails + códigos de erro estáveis).
- [ ] **[backend][P]** Converter todas as datas para UTC na serialização JSON.
- [ ] **[infra][P]** Configurar CORS e HTTPS redirection.
- [ ] **[frontend][M]** Inicializar app Angular.
- [ ] **[frontend][P]** Configurar fuso de Brasília (−03:00) no `DatePipe`.

---

## Épico 1 — Autenticação e Usuários

- [ ] **[backend][M]** Definir entidade `User` (e-mail, nome, hash de senha, papel, ativo).
- [ ] **[backend][P]** Enum `UserRole` (`Member`, `PavilionManager`, `Admin`).
- [ ] **[backend][P]** Serviço de hash de senha (PBKDF2).
- [ ] **[backend][M]** `POST /api/auth/login` — autenticação e emissão de JWT.
- [ ] **[backend][P]** `POST /api/auth/register` — cadastro de Member.
- [ ] **[backend][M]** CRUD de usuários (`/api/users`) restrito a Admin.
- [ ] **[backend][P]** Implementar redefinição de senha
- [ ] **[frontend][M]** Tela de login e fluxo de autenticação (guard + interceptor JWT).
- [ ] **[frontend][M]** Tela de gestão de usuários (Admin).
- [ ] **[backend][P]** Guards de autorização por papel nos endpoints.

---

## Épico 2 — Cadastro de Espaços (Pavilhão → Andar → Sala → Mesa)

- [ ] **[backend][M]** Entidade `Pavilion` (código, nome, política de antecedência/slot).
- [ ] **[backend][P]** Entidade `Floor` (código, nome, nível, chave do SVG).
- [ ] **[backend][P]** Entidade `Room` (external id `ROOM-`, reservável, capacidade).
- [ ] **[backend][P]** Entidade `Desk` (external id `DESK-`, reservável).
- [ ] **[backend][M]** Entidade `OperatingHours` (horário por dia da semana).
- [ ] **[backend][M]** Endpoints de listagem: pavilhões, andares e recursos do andar.
- [ ] **[backend][M]** Seed com dados reais dos pavilhões (BCP01, BCP02).
- [ ] **[frontend][M]** Listagem/seleção de pavilhão e andar.
- [ ] **[docs][P]** Documentar convenção de `ExternalId` (`ROOM-…`, `DESK-…`).

---

## Épico 3 — Gestores de Pavilhão

- [ ] **[backend][M]** Entidade de associação `PavilionManager` (N:N usuário↔pavilhão).
- [ ] **[backend][P]** `POST /api/pavilions/{id}/managers` — designar gestor (Admin).
- [ ] **[backend][P]** `DELETE /api/pavilions/{id}/managers/{userId}` — remover gestor (Admin).
- [ ] **[backend][M]** Regra de autorização por pavilhão (gestor só atua nos seus).
- [ ] **[frontend][M]** Tela de designação de gestores por pavilhão (Admin).

---

## Épico 4 — Planta Interativa (SVG)

- [ ] **[frontend][G]** Componente de planta em SVG por andar.
- [ ] **[frontend][M]** Mapear recursos (salas/mesas) para elementos do SVG via `ExternalId`.
- [ ] **[frontend][M]** Estados visuais: disponível, indisponível, bloqueado, selecionado.
- [ ] **[frontend][M]** Seleção de recurso pela planta e integração com o formulário de reserva.
- [ ] **[infra][P]** Servir arquivos SVG das plantas (`public/floors`).

---

## Épico 5 — Disponibilidade e Reserva

- [ ] **[backend][M]** Value object `TimeRange` (validação UTC, sobreposição).
- [ ] **[backend][P]** Enum `ResourceType` e `ReservationStatus`.
- [ ] **[backend][M]** Entidade `Reservation` com transições de estado.
- [ ] **[backend][G]** Serviço de disponibilidade (conflitos hierárquicos sala ⇄ mesas + bloqueios + horário).
- [ ] **[backend][P]** `POST .../availability` — verificação de disponibilidade do andar.
- [ ] **[backend][G]** Serviço de criação de reserva (validações de política, conflito, bloqueio).
- [ ] **[backend][P]** `POST /api/reservations` — solicitar reserva (status Pendente).
- [ ] **[backend][P]** `GET /api/reservations/mine` — minhas reservas.
- [ ] **[backend][P]** `POST /api/reservations/{id}/cancel` — cancelar reserva.
- [ ] **[frontend][G]** Tela de reserva (seleção de período + recurso + confirmação).
- [ ] **[frontend][M]** Tela "Minhas reservas" (listagem por status + cancelar).

---

## Épico 6 — Aprovação (Gestão)

- [ ] **[backend][M]** `GET /api/manager/reservations/pending` — pendentes dos pavilhões geridos.
- [ ] **[backend][P]** `POST /api/manager/reservations/{id}/approve` — aprovar.
- [ ] **[backend][P]** `POST /api/manager/reservations/{id}/reject` — rejeitar (com motivo).
- [ ] **[backend][P]** `POST /api/manager/reservations/{id}/revoke` — revogar (com motivo).
- [ ] **[frontend][M]** Tela de aprovações (aprovar/rejeitar/revogar com motivo).

---

## Épico 7 — Bloqueios Administrativos

- [ ] **[backend][M]** Entidade `Block` e enum `BlockTargetType` (Pavilion/Room/Desk).
- [ ] **[backend][M]** Serviço de bloqueio (resolução do pavilhão + autorização do gestor).
- [ ] **[backend][P]** `POST /api/manager/blocks` — criar bloqueio.
- [ ] **[backend][P]** `GET /api/manager/blocks` — listar bloqueios (filtros).
- [ ] **[backend][P]** Integrar bloqueios à verificação de disponibilidade.
- [ ] **[frontend][M]** Tela de listagem de bloqueios.
- [ ] **[frontend][M]** Tela de criação de bloqueio.

---

## Épico 8 — Qualidade, Documentação e Entrega

- [ ] **[backend][M]** Testes unitários do domínio (disponibilidade, transições de reserva).
- [ ] **[backend][P]** Testes de integração dos principais endpoints.
- [ ] **[backend][P]** Documentação OpenAPI/Swagger.
- [ ] **[docs][P]** README com passos de execução (API + front + banco).
- [ ] **[docs][P]** Diagramas (domínio e estados) no repositório.
- [ ] **[infra][P]** Script/checklist de deploy.
- [ ] **[docs][M]** Redação da seção de resultados para o TCC.

---

## Resumo dos épicos

| # | Épico                              | Foco principal            |
| - | ---------------------------------- | ------------------------- |
| 0 | Fundação e Infraestrutura          | Setup, Docker, CI base    |
| 1 | Autenticação e Usuários            | JWT, papéis, CRUD         |
| 2 | Cadastro de Espaços                | Hierarquia física + seed  |
| 3 | Gestores de Pavilhão               | Associação N:N + autorização |
| 4 | Planta Interativa (SVG)            | Seleção visual            |
| 5 | Disponibilidade e Reserva          | Núcleo do sistema         |
| 6 | Aprovação (Gestão)                 | Fluxo do gestor           |
| 7 | Bloqueios Administrativos          | Manutenção/eventos        |
| 8 | Qualidade, Documentação e Entrega  | Testes, docs, TCC         |
