# Alloca Site

Frontend web do **Alloca** — SPA construída com **Angular 21**, **PrimeNG**, **Tailwind** e **Bootstrap**.

> Para visão geral do produto, consulte o [README raiz](../README.md). Para arquitetura geral, veja [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md).

---

## 📑 Sumário

- [Stack](#stack)
- [Como rodar](#como-rodar)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Rotas](#rotas)
- [Configuração](#configuração)
- [Padrões e convenções](#padrões-e-convenções)
- [Internacionalização](#internacionalização)
- [Scripts npm](#scripts-npm)

---

## Stack

| Recurso              | Tecnologia                                  |
|----------------------|---------------------------------------------|
| Framework            | Angular 21 (standalone, **zoneless**)       |
| Reatividade          | Signals (`signal`, `computed`, `input`, `output`) |
| Control flow         | Novo (`@if`, `@for`, `@switch`)             |
| UI                   | PrimeNG 21 (tema Aura)                      |
| CSS utilitário       | Tailwind CSS 4 (PostCSS) + Bootstrap 5      |
| Ícones               | PrimeIcons 7                                |
| i18n                 | ngx-translate 17                            |
| Charts               | Chart.js 4                                  |
| Editor               | Quill 2                                     |
| Build                | Angular CLI 21                              |

---

## Como rodar

Pré-requisitos: **Node 20+** e **npm 10+**.

```bash
npm install
npm start
```

App disponível em `http://localhost:4200`.

> A API precisa estar rodando em `https://localhost:7112`. Veja [`alloca-api/README.md`](../alloca-api/README.md).

---

## Estrutura de pastas

```
alloca-site/
├── angular.json
├── package.json
├── tailwind.config / postcss
├── tsconfig*.json
├── public/
│   ├── floors/                # SVGs dos andares (renderizados pelo floor-map)
│   └── i18n/                  # pt-BR.json e en-US.json
└── src/
    ├── main.ts
    ├── index.html
    ├── app.component.ts
    ├── app.config.ts          # Providers globais (i18n, http, primeng, router)
    ├── app.routes.ts          # Rotas raiz
    ├── environments/
    │   ├── environment.ts
    │   └── environment.prod.ts
    ├── assets/
    │   ├── styles.scss
    │   ├── tailwind.css
    │   ├── layout/            # Layout SCSS (topbar, sidebar, menu, …)
    │   └── custom/            # Customizações por tema
    └── app/
        ├── core/                       # Infra transversal agrupada por capacidade
        │   ├── auth/
        │   │   ├── auth.service.ts
        │   │   ├── guards/             # auth, role, redirect-by-role
        │   │   ├── interceptors/       # auth.interceptor
        │   │   └── models/             # auth.model (UserRole, AuthUser, Login*)
        │   ├── feature-flags/
        │   │   └── guards/             # feature.guard
        │   ├── http/                   # error.interceptor, language.interceptor
        │   ├── i18n/                   # language.service
        │   └── layout/                 # layout.service, storage-keys
        ├── shared/                     # UI e serviços compartilhados entre features
        │   ├── components/
        │   │   ├── app-layout/         # app.layout/topbar/sidebar/menu/menuitem/footer
        │   │   ├── page-hero/
        │   │   ├── loader/
        │   │   ├── empty-state/
        │   │   └── floor-map/
        │   ├── pages/notfound/
        │   └── services/counters.service.ts
        ├── features/                   # Domínios funcionais (lazy-loaded)
        │   ├── auth/                   # pages login/access/error + auth.routes
        │   ├── pavilions/              # models + services
        │   ├── reservations/           # models, services, components, pages reserve & my-reservations
        │   ├── approvals/              # services + pages
        │   ├── blocks/                 # models, services, pages blocks & block-create
        │   ├── users/                  # models, services, pages
        │   ├── dashboard/              # pages
        │   └── history/                # pages
        └── routes/                     # Roteamento por papel
            ├── student.routes.ts
            └── manager.routes.ts
```

> Convenções:
> - Cada feature segue `{models, services, components, pages}` conforme a necessidade.
> - Páginas em pasta própria com sufixo `.page.ts` (ex.: `pages/login/login.page.ts`).
> - Sem arquivos `index.ts` (barrels) — imports apontam direto ao arquivo, favorecendo tree-shaking.
> - Imports absolutos via alias `@/` (`@/app/...`, `@/environments/...`) configurado em `tsconfig.json`.

---

## Rotas

Topo (`app.routes.ts`):

| Rota         | Layout       | Guards                                                | Conteúdo                              |
|--------------|--------------|-------------------------------------------------------|---------------------------------------|
| `/`          | —            | `redirectByRoleGuard`                                 | Redireciona conforme papel            |
| `/auth/*`    | —            | —                                                     | `login`, `access`, `error`            |
| `/student/*` | `AppLayout`  | `authGuard`, `roleGuard(['Member', 'Admin'])`         | Reserva e Minhas reservas             |
| `/manager/*` | `AppLayout`  | `authGuard`, `roleGuard(['PavilionManager','Admin'])` | Painel, Aprovações, Bloqueios, …      |
| `/notfound`  | —            | —                                                     | 404                                   |
| `**`         | —            | —                                                     | Redireciona para `/notfound`          |

### Student (`/student/*`)

| Rota                       | Componente                  |
|----------------------------|-----------------------------|
| `/student/reserve`         | `ReservePage`               |
| `/student/reservations`    | `MyReservationsPage`        |
| `/student/checkin`         | redirect → `reservations`   |

### Manager (`/manager/*`)

| Rota                       | Componente             | Guards extras                                            |
|----------------------------|------------------------|----------------------------------------------------------|
| `/manager/`                | `DashboardPage`        | —                                                        |
| `/manager/approvals`       | `ApprovalsPage`        | —                                                        |
| `/manager/blocks`          | `BlocksPage`           | —                                                        |
| `/manager/blocks/novo`     | `BlockCreatePage`      | —                                                        |
| `/manager/history`         | `HistoryPage`          | `featureGuard('managerHistory')`                         |
| `/manager/users`           | `UsersPage`            | `roleGuard(['Admin'])`, `featureGuard('managerUsers')`   |

---

## Configuração

### `src/environments/environment.ts` (dev)

```typescript
export const environment = {
    production: false,
    apiBaseUrl: 'https://localhost:7112/api',
    features: {
        managerUsers: true,
        managerHistory: false,
        notifications: false
    }
};
```

### Feature flags

A flag é consumida pelo `featureGuard(nome)` e habilita/desabilita rotas inteiras:

```typescript
{
  path: 'history',
  canMatch: [featureGuard('managerHistory')],
  loadComponent: () => import('./pages/history/history.page').then(m => m.HistoryPage)
}
```

### `app.config.ts` (resumo)

```typescript
provideRouter(appRoutes, withInMemoryScrolling(...), withEnabledBlockingInitialNavigation()),
provideHttpClient(withFetch(), withInterceptors([languageInterceptor, authInterceptor, errorInterceptor])),
provideZonelessChangeDetection(),
provideTranslateService({
    fallbackLang: 'pt-BR',
    lang: lerIdiomaAtual(),
    loader: provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' })
}),
providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } } })
```

---

## Padrões e convenções

### Componentes

- **Standalone** sempre. Sem `NgModule`.
- **Signals** como modelo reativo (`signal()`, `computed()`).
- **Inputs/outputs** declarados via funções (`input()`, `output()`) — sem decoradores.
- **Control flow** novo (`@if`, `@for`, `@switch`) — não usar `*ngIf`/`*ngFor`.

### Nomes (preferência do projeto)

| Tipo                | Padrão                                                    |
|---------------------|-----------------------------------------------------------|
| Variáveis/funções   | Português (`mudancaSelecao`, `carregarLista()`)           |
| Inputs/Outputs      | Português (`multiplo`, `desabilitado`, `mudancaBusca`)    |
| Arquivos            | `kebab-case` em português (mantendo sufixos `.component.ts`, `.service.ts`, `.page.ts`) |
| APIs do framework   | Inglês (`ngOnInit`, `writeValue`, `Validators.required`)  |
| Classes utilitárias | Inglês (Tailwind, Bootstrap, `form-control`, `is-invalid`) |

### Serviços

- Sempre `@Injectable({ providedIn: 'root' })`.
- Estado exposto via `signal()` (privado `signal`, público `readonly`).
- Comunicação HTTP via `HttpClient` injetado por `inject(HttpClient)`.
- Interceptors centralizam `Authorization`, `Accept-Language` e tratamento de erros.

### Guards

```typescript
export const authGuard: CanMatchFn = () => { /* … */ };
export const roleGuard = (papeisPermitidos: string[]): CanMatchFn => () => { /* … */ };
export const featureGuard = (nome: keyof Environment['features']): CanMatchFn => () => { /* … */ };
```

---

## Internacionalização

- Arquivos: `public/i18n/pt-BR.json` e `public/i18n/en-US.json`.
- Idioma persistido em `localStorage` (chave em `core/layout/storage-keys.ts`).
- `languageInterceptor` injeta `Accept-Language` em todas as requisições.
- Tradução de PrimeNG aplicada no `provideAppInitializer`:

  ```typescript
  const traducaoPrimeng = translate.instant('primeng');
  if (traducaoPrimeng && typeof traducaoPrimeng === 'object') {
      primeng.setTranslation(traducaoPrimeng);
  }
  ```

Detalhes e convenções em [`docs/i18n-conventions.md`](./docs/i18n-conventions.md).

---

## Scripts npm

| Script             | Descrição                                            |
|--------------------|------------------------------------------------------|
| `npm start`        | Dev server em `http://localhost:4200`                |
| `npm run build`    | Build de produção em `dist/alloca-ng/`               |
| `npm run watch`    | Build em modo desenvolvimento com watch              |
| `npm test`         | Roda Karma + Jasmine                                 |
| `npm run format`   | Prettier em `**/*.{js,ts,html}`                      |

---

## Acessibilidade & UX

- Sempre que possível, componentes interativos devem ter `aria-label` traduzido via i18n.
- Indicadores de carregamento usam o componente compartilhado `loader`.
- Estados vazios usam o componente `empty-state` com mensagem traduzida.
