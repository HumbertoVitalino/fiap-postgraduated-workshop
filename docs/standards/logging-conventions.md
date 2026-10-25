# Logging — convenções

## Setup

- Use **Serilog** (`Serilog.AspNetCore`) no lugar do provider padrão do `Microsoft.Extensions.Logging`, via `builder.Host.UseSerilog(...)` no `Program.cs`.
- **Bootstrap logger**: crie um logger estático mínimo *antes* do host ser construído, e envolva a montagem/execução inteira do host num `try/catch/finally`. Isso captura falhas de startup (ex.: banco indisponível durante uma migration automática, configuração inválida) que aconteceriam antes mesmo do container de DI terminar de montar — sem isso, essas falhas saem como stack trace cru no console ou, pior, silenciosamente.
- `app.UseSerilogRequestLogging()` para logar cada requisição HTTP (método, rota, status, duração) sem precisar instrumentar cada endpoint manualmente.
- Configuração de sink (Console, arquivo, serviço externo) fica em configuração (`appsettings.json`, seção `Serilog`, lida via `ReadFrom.Configuration`) — nunca hardcoded no código.
- **Nada disso muda o código de Application/Domain.** Todo `ILogger<T>` já injetado nos use cases continua funcionando sem alteração — a troca de provider é só na composição (`Program.cs`), nunca no código de negócio.

## Onde logar

`ILogger<T>` é injetado no construtor de toda classe de use case (Application). Domain **não** loga — é uma camada sem dependências externas, log é uma preocupação de infraestrutura/aplicação, não de regra de negócio pura.

## O que logar em cada nível

- **`LogWarning`** — falha **esperada**, de regra de negócio, que vai virar uma resposta 4xx para o cliente: registro não encontrado, valor duplicado, estado inválido para a transição pedida, credencial inválida. Não é uma falha do sistema, é o sistema recusando corretamente uma operação inválida.
- **`LogError`** — falha **inesperada**, de infraestrutura: commit no banco falhou, dependência externa não respondeu, exceção não tratada. Indica algo que precisa de atenção operacional, não só uma resposta amigável ao cliente.
- Nunca use `LogInformation`/`LogDebug` para o caminho de sucesso de todo use case por padrão — isso vira ruído. Reserve para pontos de decisão que valham ser rastreados (ex. um dispatch de evento, um job em background).

## Segurança: não logue segredo nem dado sensível

- Nunca logue senha em texto plano, token, hash de senha, ou qualquer campo classificado como PII sensível diretamente. Ferramentas de SAST (CodeQL, SonarQube) sinalizam isso como achado de severidade média/alta — "exposição de dado sensível em log".
- **Log forging**: nunca interpole diretamente um valor vindo do usuário dentro do template da mensagem de log de um jeito que permita injetar quebras de linha/caracteres de controle e forjar entradas de log falsas. Use os parâmetros estruturados do logger (`_logger.LogWarning("User with email {Email} already exists.", email)`), nunca concatenação de string (`_logger.LogWarning("User with email " + email + " already exists.")`) — além de mais seguro, os parâmetros estruturados ficam pesquisáveis/filtráveis no sink.

## Testando log

Moq (e a maioria das libs de mock) não consegue mockar/verificar `LogWarning`/`LogError` diretamente porque são *extension methods* — é preciso verificar a chamada real ao método `ILogger.Log<TState>` por trás deles. Um helper reutilizável:

```csharp
public abstract class LoggerTestBase<TCategory>
{
    protected readonly Mock<ILogger<TCategory>> LoggerMock = new();

    protected void VerifyLog(LogLevel level, string message, Times times)
    {
        LoggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString() == message),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times
        );
    }
}
```

Toda classe de teste de use case que precisa confirmar um `LogWarning`/`LogError` específico herda de `LoggerTestBase<TUseCase>` e chama `VerifyLog(LogLevel.Warning, "mensagem exata", Times.Once())`.
