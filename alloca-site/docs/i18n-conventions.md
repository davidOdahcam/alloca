# Convenção de chaves i18n

Este documento define o padrão de nomenclatura para as chaves dos arquivos
`public/i18n/<locale>.json` (atualmente `pt-BR.json` e `en-US.json`).

## TL;DR

- **Chaves de UI**: `camelCase`, hierarquia por pontos.
- **Códigos de erro** (`errors.codes.*`): espelham literalmente o
  `ErrorCodes` do backend (`domínio.snake_case`).
- **Tags de idioma** (`language.<tag>`): seguem o padrão BCP-47
  (`pt-BR`, `en-US`).

## 1. Estrutura geral

Cada chave é um caminho com pontos, do mais genérico ao mais específico:

```
<feature>.<sub-feature>.<elemento>
```

Exemplos válidos:

```jsonc
"common.actions.save"
"reservations.reserve.dialog.confirm"
"blocks.create.errors.endBeforeStart"
```

### Hierarquia recomendada

| Nível 1                | Conteúdo                                                          |
| ---------------------- | ----------------------------------------------------------------- |
| `app`                  | Identidade do produto (nome, tagline)                             |
| `common`               | Termos genéricos reutilizados: `actions`, `status`, `labels`, ... |
| `roles`                | Papéis (`admin`, `manager`, `member`)                             |
| `topbar` / `menu`      | Layout (cabeçalho, navegação)                                     |
| `auth`                 | Páginas de autenticação                                           |
| `users` / `blocks` /   | Features de domínio                                               |
| `reservations` / ...   |                                                                   |
| `errors`               | Mensagens de erro técnico e códigos do backend                    |
| `validation`           | Mensagens de validação de formulário                              |
| `language`             | Nomes de idiomas exibidos no seletor                              |
| `primeng`              | Overrides de strings da PrimeNG                                   |

Dentro de cada feature, agrupe por subseção:

```jsonc
"reservations": {
    "reserve":   { /* tela de reservar */ },
    "approvals": { /* tela de aprovações */ },
    "myList":    { /* tela "minhas reservas" */ },
    "toasts":    { /* toasts compartilhados entre as três */ }
}
```

## 2. Estilo de cada segmento (UI)

- **`camelCase`** estrito: começa com minúscula, palavras concatenadas com
  inicial maiúscula a partir da segunda. Dígitos são permitidos
  (`next24h`, `step1`).
- Sem `_`, sem `-`, sem espaço, sem acento.
- **Em inglês**, mesmo com a UI sendo bilíngue. As traduções vão nos
  *valores*, não nas chaves.
- Sufixos padronizados:

  | Sufixo / chave   | Significado                                                         |
  | ---------------- | ------------------------------------------------------------------- |
  | `*Label`         | Rótulo de campo (`pavilionLabel`, `dateLabel`)                      |
  | `*Placeholder`   | Placeholder de input/select                                         |
  | `*Title`         | Título de bloco/diálogo                                             |
  | `*Description` / | Texto auxiliar abaixo de um título                                  |
  | `*Subtitle`      |                                                                     |
  | `*Tooltip`       | Texto de tooltip                                                    |
  | `*Hint`          | Mensagem auxiliar curta                                             |
  | `empty` / `emptyDescription` | Estado vazio                                            |
  | `loading`        | Mensagem de carregamento                                            |
  | `confirm.title` / `confirm.message` / `confirm.acceptLabel` | Diálogos de confirmação |
  | `toasts.<acao>`  | Toast de sucesso/erro de uma ação                                   |
  | `columns.<campo>`| Cabeçalhos de tabela                                                |

## 3. Erros do backend (`errors.codes.*`)

Esta seção é **diferente**: as chaves precisam coincidir literalmente com
as constantes públicas em
[`ErrorCodes.cs`](../../alloca-api/src/Alloca.Application/Common/Exceptions/ErrorCodes.cs)
porque são emitidas pelo middleware no campo `code` de cada
`ProblemDetails`.

Padrão do backend (e portanto também das chaves):

```
<dominio>.<acao_em_snake_case>
```

Exemplos:

```jsonc
"errors": {
    "codes": {
        "user.email.in_use":            "Já existe um usuário com este e-mail.",
        "user.invalid_credentials":     "E-mail ou senha incorretos.",
        "reservation.past_date":        "Não é possível reservar em data passada.",
        "reservation.min_advance":      "É preciso ter pelo menos {{0}} de antecedência."
    }
}
```

> Regra: se a constante no backend mudar, a chave no JSON muda junto.
> Não traduza, não normalize, não converta para camelCase.

Mensagens curtas/sem código (rede, timeout, etc.) ficam em `errors.<nome>`
(`errors.network`, `errors.server`) e seguem a regra normal de UI.

## 4. Interpolação

`ngx-translate` usa `{{nome}}` para parâmetros nomeados:

```jsonc
"deskCount": "{{count}} mesa(s)",
"approvedBatch": { "summary": "{{count}} aprovadas" }
```

Para mensagens vindas do backend com argumentos posicionais, usamos
`{{0}}`, `{{1}}`, etc. — o `error.interceptor.ts` faz a conversão
`args[]` → `{0: ..., 1: ...}` antes de passar para o `translate.instant`.

## 5. Tags de idioma (`language.*`)

A subseção `language` usa **BCP-47** como chave para os nomes exibidos
no seletor de idioma:

```jsonc
"language": {
    "description": "Idioma da interface",
    "pt-BR": "Português (Brasil)",
    "en-US": "English (US)"
}
```

Essas hifenizações são intencionais e **não** violam a regra do
`camelCase`: são identificadores externos padronizados (RFC 5646).

## 6. Convenção do código TypeScript

- Em templates: `{{ 'feature.x.y' | translate }}` ou
  `[label]="'feature.x.y' | translate"`.
- Em código: `this.translate.instant('feature.x.y', { param: valor })`.
- Para strings reativas ao idioma, usar `computed()` declarando dependência
  com `void this.language.atual()`.
- Para formatação local (`Intl.DateTimeFormat`, `Intl.NumberFormat`),
  passar `this.language.atual()` como locale, **não** literais
  `'pt-BR'` / `'en-US'`.

## 7. Checklist ao adicionar uma nova chave

1. A chave começa com uma feature já existente? Reaproveite a hierarquia.
2. O segmento é `camelCase`, em inglês, sem acento, sem `_`/`-`?
3. Existe equivalente em `pt-BR.json` **e** `en-US.json`?
4. Se for um sufixo conhecido (`Label`, `Placeholder`, ...), está sendo
   usado de acordo com a tabela acima?
5. Se a chave mapear um `ErrorCode` do backend, ela está exatamente igual
   à constante (incluindo `snake_case` e pontos internos)?
