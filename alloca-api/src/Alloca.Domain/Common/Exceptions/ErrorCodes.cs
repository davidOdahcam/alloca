namespace Alloca.Domain.Common.Exceptions;

/// <summary>
/// Códigos estáveis de erro consumidos pelo frontend para tradução via i18n.
/// O frontend resolve cada código em <c>errors.codes.&lt;code&gt;</c>.
/// </summary>
public static class ErrorCodes
{
    // Genéricos
    public const string Unknown = "unknown";
    public const string Unauthenticated = "auth.unauthenticated";
    public const string ValidationError = "validation.error";

    // Usuários / autenticação
    public const string UserNotFound = "user.not_found";
    public const string UserEmailInUse = "user.email.in_use";
    public const string UserInvalidCredentials = "user.invalid_credentials";
    public const string UserInactive = "user.inactive";
    public const string UserInvalidRole = "user.invalid_role";
    public const string UserCannotDemoteSelf = "user.cannot_demote_self";
    public const string UserCannotDeactivateSelf = "user.cannot_deactivate_self";
    public const string UserSuspended = "user.suspended";

    // Pavilhões / andares / salas / mesas
    public const string PavilionNotFound = "pavilion.not_found";
    public const string FloorNotFound = "floor.not_found";
    public const string RoomNotFound = "room.not_found";
    public const string RoomNotReservable = "room.not_reservable";
    public const string DeskNotFound = "desk.not_found";
    public const string DeskNotReservable = "desk.not_reservable";

    // Reservas
    public const string ReservationNotFound = "reservation.not_found";
    public const string ReservationMinDuration = "reservation.min_duration";
    public const string ReservationMaxDuration = "reservation.max_duration";
    public const string ReservationMinAdvance = "reservation.min_advance";
    public const string ReservationMaxAdvance = "reservation.max_advance";
    public const string ReservationOutsideHours = "reservation.outside_hours";
    public const string ReservationSlotAlignment = "reservation.slot_alignment";
    public const string ReservationActiveLimit = "reservation.active_limit";
    public const string ReservationPavilionBlocked = "reservation.pavilion_blocked";
    public const string ReservationRoomBlocked = "reservation.room_blocked";
    public const string ReservationResourceBlocked = "reservation.resource_blocked";
    public const string ReservationConflict = "reservation.conflict";
    public const string ReservationOwnerOnly = "reservation.owner_only";
    public const string ReservationCheckInWrongQr = "reservation.checkin.wrong_qr";
    public const string ReservationBusinessRule = "reservation.business_rule";

    // Bloqueios
    public const string BlockNotPavilionManager = "block.not_pavilion_manager";
    public const string BlockTargetUnknown = "block.target_unknown";

    // Gestão
    public const string ManagerNotPavilionManager = "manager.not_pavilion_manager";
}
