namespace Alloca.Domain.Common.Settings;

public class ReservationPolicySettings
{
    public int MinDurationMinutes { get; set; } = 30;
    public int MaxDurationMinutes { get; set; } = 120;
    public int MaxActiveReservations { get; set; } = 2;
    public int CancellationCutoffHours { get; set; } = 2;
}
