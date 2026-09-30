# Diretrizes de Desenvolvimento - Backend .NET

Este projeto segue uma arquitetura baseada em camadas (Clean Architecture) com foco em separação de responsabilidades, código limpo e objetividade. Ao gerar ou sugerir código, siga estritamente as regras abaixo.

## 1. Stack Tecnológica
- **Framework:** .NET 10 (Utilizar os recursos mais recentes do C#).
- **Banco de Dados:** SQL Server.
- **Object Mapping:** Mapster.

## 2. Arquitetura da Solução e Responsabilidades
A solução é dividida nos seguintes projetos. Respeite o fluxo de dependências e a responsabilidade de cada um:

*   **Domain:** O núcleo da aplicação. Não deve possuir dependências externas ou de outros projetos da solução.
    *   *Conteúdo:* Entidades de domínio, Serviços de Domínio, Interfaces de Repositórios, Enums e Value Objects.
*   **Application:** Orquestra os casos de uso. Depende exclusivamente do projeto *Domain*.
    *   *Conteúdo:* Serviços de Aplicação, classes de Request e Response (DTOs) e as configurações de mapeamento utilizando Mapster.
*   **Infra:** Comunicação com o banco de dados e recursos externos. Depende do projeto *Domain*.
    *   *Conteúdo:* Implementação dos Repositórios (que assinam as interfaces do Domain) e Modelos de Banco de Dados/Mapeamento ORM.
*   **CrossCutting:** Recursos transversais da aplicação.
    *   *Conteúdo:* Helpers, utilitários globais, tratamento de exceções padronizadas e logs.
*   **IoC:** Centraliza a injeção de dependência.
    *   *Conteúdo:* Deve conter a classe `NativeInjectorBootstrapper`. Sempre que um novo repositório ou serviço for criado, o registro de Injeção de Dependência deve ser feito nesta classe.
*   **API:** Ponto de entrada da aplicação. Depende de *Application* e *IoC*.
    *   *Conteúdo:* Controllers ou Minimal APIs. Deve ser uma camada "fina", responsável apenas por receber requisições, repassar para a camada *Application* e retornar o Response adequado. Não insira regras de negócio nesta camada.

## 3. Padrões de Código e Bibliotecas
- **Injeção de Dependências:** Utilize sempre injeção via construtor.
- **Mapeamento:** Utilize a biblioteca **Mapster** para converter Entidades em Responses ou Requests em Entidades. Evite mapeamentos manuais extensos.
- **Objetividade:** Escreva métodos concisos. Se uma lógica for complexa, extraia para métodos privados com nomes autoexplicativos.