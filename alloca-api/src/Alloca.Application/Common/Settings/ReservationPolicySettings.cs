namespace Alloca.Application.Common.Settings;

public class ReservationPolicySettings
{
    public int MinDurationMinutes { get; set; } = 30;
    public int MaxDurationMinutes { get; set; } = 120;
    public int MaxActiveReservations { get; set; } = 2;
    public int CancellationCutoffHours { get; set; } = 2;
    public int NoShowGraceMinutes { get; set; } = 15;
    public int StrikeWindowDays { get; set; } = 30;
    public int StrikeSuspensionThreshold { get; set; } = 3;
    public int StrikeSuspensionDays { get; set; } = 7;
}
