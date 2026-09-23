namespace Alloca.Domain.Services;

/// <summary>
/// Serviço de domínio que aplica as transições automáticas do ciclo de vida das reservas
/// (no-show, conclusão, strikes e suspensões). Consumido pelos jobs recorrentes.
/// </summary>
public interface IReservationLifecycleService
{
    /// <summary>Executa as transições para o instante informado e retorna os totais processados.</summary>
    Task<(int NoShows, int Completed)> RunAsync(DateTime nowUtc, CancellationToken ct = default);
}
