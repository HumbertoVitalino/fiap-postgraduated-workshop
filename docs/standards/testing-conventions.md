# Testes — convenções

## Três camadas, cada uma com um propósito distinto

```
{Solucao}.UnitTests           # Domain + Application isolados, tudo mockado, sem I/O
{Solucao}.IntegrationTests    # EF Core real contra um banco real (docker-compose de testes)
{Solucao}.FunctionalTests     # WebApplicationFactory — HTTP fim-a-fim, exercita a composição de DI da Api
```

Não trate a camada funcional/end-to-end como opcional assim que o projeto tiver: validação por `IEndpointFilter` com validators resolvidos do DI, mais de uma policy de autorização, ou qualquer lógica que dependa da composição real do `Program.cs`. Um `IValidator<T>` esquecido no registro de DI, por exemplo, não falha nem no build nem nos testes unitários/de integração — só um teste que sobe a aplicação de verdade (ou um teste manual) pega isso, porque só ele exercita `AddApi()`/`AddValidators()` de verdade.

## Escolha uma lib de mock só

Não deixe o projeto de testes referenciar duas libs de mock (ex. Moq **e** NSubstitute) "por via das dúvidas". Escolha uma, documente a escolha, e remova a outra da dependência assim que notar a duplicação — duas libs fazendo a mesma coisa é peso morto e uma fonte de inconsistência de estilo entre arquivos de teste.

## Cobertura esperada por use case

Para cada use case novo, cubra no nível unitário:
- o caminho de sucesso,
- cada branch de erro distinto que ele pode produzir (não encontrado, duplicado/conflito, falha de persistência, invariante de domínio violada propagando como exceção),
- que o log correto (`LogWarning`/`LogError`, ver `logging-conventions.md`) é emitido no branch certo.

No nível de integração, cubra pelo menos:
- persistir e reler de volta (round-trip real contra o banco),
- um caminho de falha que dependa de estado real do banco (ex. duas linhas concorrendo por um índice único).

## Armadilha do AutoFixture com invariantes cruzadas

Se o construtor de uma entidade valida uma invariante cruzando dois ou mais campos numéricos/data (ex. `CampoA >= CampoB`), **não use** `_fixture.Create<Entity>()`/`CreateMany<Entity>()` para gerar instâncias de teste — o gerador cria os dois campos de forma independente e vai estourar a validação de forma intermitente (o teste passa na maioria das execuções e falha aleatoriamente às vezes, o pior tipo de flake). Use valores fixos e mutuamente consistentes para os campos envolvidos na invariante, gerando os demais campos livremente via fixture se quiser.

## Armadilha do fixture de banco compartilhado

Quando a fixture de integração é compartilhada por toda uma collection de teste (`ICollectionFixture`) sem reset entre classes/testes, **nunca escreva um teste que dependa de uma tabela estar vazia**. Gere valores únicos (email, documento, placa, código) por teste e verifique existência/conteúdo específico, em vez de contar linhas ou assumir estado inicial. Se um cenário só faz sentido com a tabela vazia (ex. "lista vazia retorna sucesso, não erro"), cubra esse cenário só no nível unitário (mockado), não no de integração.

## Testando log

Ver `logging-conventions.md` — Moq (e libs equivalentes) não verificam `LogWarning`/`LogError` diretamente por serem extension methods; use um helper que verifique a chamada real a `ILogger.Log<TState>`.

## Sinal de alerta: build e testes verdes não significam "testado de ponta a ponta"

Um teste unitário que mocka o repositório e um teste de integração que chama o use case diretamente (sem passar pelo HTTP) não pegam bugs de composição da Api (DI faltando, filtro mal configurado, rota duplicada). Se uma feature envolve mudança na composição da Api (`Program.cs`, `IoC/DependencyInjection.cs`, um filtro novo), valide rodando a aplicação de verdade contra o ambiente de desenvolvimento antes de considerar a feature fechada — não só `dotnet build`/`dotnet test`.
