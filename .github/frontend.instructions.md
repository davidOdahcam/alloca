# Diretrizes de Desenvolvimento - Frontend (Angular)

Este projeto utiliza Angular 21 integrado com o template Sakai (baseado em PrimeNG). Ao gerar código para o frontend, siga estritamente as regras de arquitetura, modernização e UI descritas abaixo.

## 1. Stack e Práticas Modernas
- **Versão:** Angular 21.
- **Standalone Components:** Uso obrigatório de `standalone: true`. É estritamente proibido o uso de `NgModules`.
- **Signals:** O gerenciamento de estado e reatividade deve ser feito primariamente com Signals (`signal`, `computed`, `effect`).
- **Injeção de Dependência:** Utilize injeção funcional com a função `inject()`. Evite injeção de dependência via construtor.
- **Minimal Decorators:** Não utilize decorators tradicionais para inputs e outputs. Adote as APIs reativas: `input()`, `output()`, `model()`.

## 2. Estrutura de Diretórios
Respeite a separação de responsabilidades ao sugerir a criação ou modificação de arquivos:
- `app/core/`: Infraestrutura transversal e singletons, agrupada por capacidade: `auth/` (service, guards, interceptor, models de autenticação), `feature-flags/guards/`, `http/` (interceptors de erro e idioma), `i18n/` (language.service), `layout/` (layout.service, storage-keys).
- `app/shared/`: UI e serviços reutilizados por múltiplas features. Contém `components/` (ex.: `app-layout`, `page-hero`, `loader`, `empty-state`, `floor-map`), `pages/` (ex.: `notfound`) e `services/` (ex.: `counters.service`). Use PrimeNG (template Sakai) nos componentes visuais.
- `app/features/{dominio}/`: Domínios de negócio (ex.: `auth`, `pavilions`, `reservations`, `approvals`, `blocks`, `users`, `dashboard`, `history`), alinhados ao backend. Cada feature encapsula `models/`, `services/`, `components/` e `pages/` conforme necessário. Páginas devem estar em pasta própria com sufixo `.page.ts` (ex.: `pages/login/login.page.ts`).
- `app/routes/`: Roteamento por papel (`student.routes.ts`, `manager.routes.ts`) — agregam pages de várias features sob `loadComponent` com alias `@/app/features/...`.
- **Não criar arquivos `index.ts` (barrels)**: imports devem apontar diretamente para o arquivo de destino para preservar tree-shaking. Use o alias `@/` (mapeado a `src/`) em imports absolutos.

## 3. UI e Estilização (Sakai / PrimeNG)
- Utilize os componentes do PrimeNG para manter a consistência com o template Sakai.
- **Design:** Mantenha um visual minimalista, profissional e focado em usabilidade. Utilize a paleta de cores sóbria do template.
- Evite o excesso de ícones ou elementos visuais que deem à interface uma aparência automatizada ou poluída.

## 4. Padrões de Código
- Isole a lógica de negócios da visualização. Componentes devem focar no bind de dados; a lógica complexa (carregamento, formatação de dados) deve residir em services isolados.
- Escreva código objetivo, limpo e direto.