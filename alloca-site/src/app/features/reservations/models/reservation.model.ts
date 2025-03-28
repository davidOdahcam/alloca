import { ResourceType } from '@features/pavilions/models/pavilion.model';

export type ReservationStatus = 'Pending' | 'Approved' | 'Rejected' | 'CancelledByUser' | 'RevokedByManager' | 'InProgress' | 'Completed' | 'NoShow';

export interface Reservation {
    id: string;
    resourceType: ResourceType;
    roomId: string | null;
    deskId: string | null;
    resourceExternalId: string;
    resourceName: string;
    pavilionId: string;
    pavilionName: string;
    startUtc: string;
    endUtc: string;
    status: ReservationStatus;
    notes: string | null;
    decisionReason: string | null;
    checkedInAt: string | null;
    createdAt: string;
}

export interface CreateReservationRequest {
    resourceType: ResourceType;
    resourceId: string;
    startUtc: string;
    endUtc: string;
    notes?: string | null;
}

export interface CreateReservationResponse {
    id: string;
}

export interface CheckInRequest {
    scannedExternalId: string;
}
