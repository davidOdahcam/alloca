import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { ReservationStatus } from '@features/reservations/models/reservation.model';

type Severity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

export const STATUS_LABEL: Record<ReservationStatus, string> = {
    Pending: 'Pendente',
    Approved: 'Aprovada',
    Rejected: 'Recusada',
    CancelledByUser: 'Cancelada',
    RevokedByManager: 'Revogada',
    InProgress: 'Em andamento',
    Completed: 'Concluída',
    NoShow: 'Não compareceu'
};

export const RESERVATION_STATUSES: ReservationStatus[] = ['Pending', 'Approved', 'InProgress', 'Completed', 'NoShow', 'Rejected', 'CancelledByUser', 'RevokedByManager'];

const STATUS_SEVERITY: Record<ReservationStatus, Severity> = {
    Pending: 'warn',
    Approved: 'success',
    Rejected: 'danger',
    CancelledByUser: 'secondary',
    RevokedByManager: 'danger',
    InProgress: 'info',
    Completed: 'secondary',
    NoShow: 'danger'
};

@Component({
    selector: 'app-reservation-status-tag',
    standalone: true,
    imports: [CommonModule, TagModule],
    template: `<p-tag [value]="label()" [severity]="severity()" />`
})
export class ReservationStatusTag {
    readonly status = input.required<ReservationStatus>();
    readonly label = computed(() => STATUS_LABEL[this.status()]);
    readonly severity = computed(() => STATUS_SEVERITY[this.status()]);
}
