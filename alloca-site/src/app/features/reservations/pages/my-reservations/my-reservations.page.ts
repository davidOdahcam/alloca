import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { RouterModule } from '@angular/router';
import { ReservationService } from '@features/reservations/services/reservation.service';
import { LanguageService } from '@core/i18n/language.service';
import { Reservation, ReservationStatus } from '@features/reservations/models/reservation.model';
import { ReservationStatusTag, RESERVATION_STATUSES, STATUS_LABEL } from '@features/reservations/components/reservation-status-tag/reservation-status-tag';
import { PageHero } from '@shared/components/page-hero/page-hero';
import { Loader } from '@shared/components/loader/loader';
import { EmptyState } from '@shared/components/empty-state/empty-state';

const STATUS_PROXIMAS: ReservationStatus[] = ['Approved', 'InProgress'];

@Component({
    selector: 'app-my-reservations',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        TranslatePipe,
        ButtonModule,
        ConfirmDialogModule,
        DialogModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule,
        MultiSelectModule,
        TableModule,
        TooltipModule,
        ReservationStatusTag,
        PageHero,
        Loader,
        EmptyState
    ],
    providers: [ConfirmationService],
    template: `
        <p-confirmDialog />

        <app-page-hero [title]="'reservations.myList.title' | translate" [description]="'reservations.myList.subtitle' | translate" [breadcrumb]="breadcrumb()">
            <p-button [label]="'reservations.myList.newReservation' | translate" icon="pi pi-plus" routerLink="/student/reserve" />
        </app-page-hero>

        @if (loading()) {
            <div class="card"><app-loader [rotulo]="'reservations.myList.loading' | translate" /></div>
        } @else {
            @if (proximaReserva(); as r) {
                <article class="proxima-card">
                    <div class="proxima-card__topo">
                        <span class="proxima-card__badge">
                            <i class="pi pi-clock"></i>
                            {{ contagemRegressiva(r) }}
                        </span>
                        <app-reservation-status-tag [status]="r.status" />
                    </div>

                    <div class="proxima-card__corpo">
                        <div class="proxima-card__principal">
                            <h3 class="proxima-card__titulo">{{ r.resourceName }}</h3>
                            <div class="proxima-card__codigo">{{ r.resourceExternalId }}</div>
                            <div class="proxima-card__meta">
                                <span><i class="pi pi-map-marker"></i> {{ r.pavilionName }}</span>
                                <span>
                                    <i class="pi pi-calendar"></i>
                                    {{ r.startUtc | date: "EEEE, dd 'de' MMMM" }}
                                </span>
                                <span>
                                    <i class="pi pi-hourglass"></i>
                                    {{ r.startUtc | date: 'HH:mm' }} – {{ r.endUtc | date: 'HH:mm' }}
                                </span>
                            </div>
                        </div>

                        <div class="proxima-card__acoes">
                            @if (canShowQr(r)) {
                                <p-button [label]="'reservations.myList.showQr' | translate" icon="pi pi-qrcode" (onClick)="openQr(r)" />
                            }
                            @if (canCancel(r)) {
                                <p-button
                                    [label]="'reservations.myList.cancelReservation' | translate"
                                    icon="pi pi-times"
                                    severity="danger"
                                    [outlined]="true"
                                    [pTooltip]="cancelTooltip(r)"
                                    [disabled]="!podeCancelarAgora(r)"
                                    (onClick)="confirmCancel(r)"
                                />
                            }
                        </div>
                    </div>
                </article>
            }

            <div class="card">
                <div class="reservas-toolbar">
                    <p-iconfield iconPosition="left" styleClass="reservas-busca">
                        <p-inputicon><i class="pi pi-search"></i></p-inputicon>
                        <input pInputText type="text" [placeholder]="'reservations.myList.searchPlaceholder' | translate" [(ngModel)]="searchText" class="w-full" />
                    </p-iconfield>
                    <p-multiselect
                        [options]="opcoesStatus"
                        optionLabel="label"
                        optionValue="value"
                        [(ngModel)]="statusSelecionados"
                        [placeholder]="'reservations.myList.filterStatus' | translate"
                        [selectedItemsLabel]="'reservations.myList.filterStatusSelected' | translate"
                        [maxSelectedLabels]="2"
                        [showClear]="true"
                        appendTo="body"
                        styleClass="reservas-filtro-status"
                    />
                </div>

                <ng-container *ngTemplateOutlet="lista; context: { $implicit: filtrar(reservations()) }" />
            </div>
        }

        <ng-template #lista let-items>
            @if (items.length === 0) {
                <app-empty-state icone="pi pi-inbox" [titulo]="'reservations.myList.empty' | translate" [descricao]="'reservations.myList.emptyDescription' | translate" [compact]="true" />
            } @else {
                <p-table [value]="items" dataKey="id" [paginator]="items.length > 10" [rows]="10" responsiveLayout="scroll" sortField="startUtc" [sortOrder]="-1">
                    <ng-template pTemplate="header">
                        <tr>
                            <th pSortableColumn="resourceName">{{ 'reservations.myList.columns.resource' | translate }} <p-sortIcon field="resourceName" /></th>
                            <th>{{ 'reservations.myList.columns.pavilion' | translate }}</th>
                            <th pSortableColumn="startUtc">{{ 'reservations.myList.columns.period' | translate }} <p-sortIcon field="startUtc" /></th>
                            <th>{{ 'reservations.myList.columns.status' | translate }}</th>
                            <th class="text-right">{{ 'reservations.myList.columns.actions' | translate }}</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-r>
                        <tr>
                            <td>
                                <div class="font-medium">{{ r.resourceName }}</div>
                                <div class="text-xs text-muted-color">{{ r.resourceExternalId }}</div>
                            </td>
                            <td>{{ r.pavilionName }}</td>
                            <td>
                                <div>{{ r.startUtc | date: 'dd/MM/yyyy HH:mm' }}</div>
                                <div class="text-xs text-muted-color">{{ 'reservations.myList.until' | translate }} {{ r.endUtc | date: 'HH:mm' }}</div>
                            </td>
                            <td><app-reservation-status-tag [status]="r.status" /></td>
                            <td class="text-right whitespace-nowrap">
                                @if (canShowQr(r)) {
                                    <p-button icon="pi pi-qrcode" [text]="true" [rounded]="true" [pTooltip]="'reservations.myList.qrTooltip' | translate" [ariaLabel]="'reservations.myList.showQr' | translate" (onClick)="openQr(r)" />
                                }
                                @if (canCancel(r)) {
                                    <p-button
                                        icon="pi pi-times"
                                        severity="danger"
                                        [text]="true"
                                        [rounded]="true"
                                        [pTooltip]="cancelTooltip(r)"
                                        [ariaLabel]="'reservations.myList.cancel' | translate"
                                        [disabled]="!podeCancelarAgora(r)"
                                        (onClick)="confirmCancel(r)"
                                    />
                                }
                            </td>
                        </tr>
                    </ng-template>
                </p-table>
            }
        </ng-template>

        <p-dialog [visible]="!!qrUrl()" (visibleChange)="$event || closeQr()" [modal]="true" [closable]="true" [style]="{ width: '24rem' }" [header]="'reservations.myList.qrTitle' | translate">
            @if (qrUrl(); as url) {
                <div class="flex flex-col items-center gap-2">
                    <img [src]="url" alt="QR Code" style="width: 100%; max-width: 280px;" />
                    <p class="text-muted-color text-sm m-0 text-center">
                        {{ 'reservations.myList.qrInstruction' | translate }}
                    </p>
                </div>
            }
        </p-dialog>
    `,
    styles: [
        `
            .proxima-card {
                position: relative;
                margin-bottom: 1.25rem;
                padding: 1.25rem 1.4rem;
                border-radius: 0.85rem;
                background: linear-gradient(135deg, color-mix(in srgb, var(--primary-color), transparent 88%) 0%, color-mix(in srgb, var(--primary-color), transparent 95%) 100%);
                border: 1px solid color-mix(in srgb, var(--primary-color), transparent 75%);
                overflow: hidden;
            }
            .proxima-card::before {
                content: '';
                position: absolute;
                inset: 0 auto 0 0;
                width: 4px;
                background: var(--primary-color);
            }
            .proxima-card__topo {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 0.85rem;
                gap: 0.5rem;
                flex-wrap: wrap;
            }
            .proxima-card__badge {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                padding: 0.3rem 0.7rem;
                border-radius: 999px;
                background: var(--primary-color);
                color: var(--primary-contrast-color, #fff);
                font-size: 0.75rem;
                font-weight: 600;
                text-transform: uppercase;
                letter-spacing: 0.04em;
            }
            .proxima-card__corpo {
                display: flex;
                gap: 1.25rem;
                justify-content: space-between;
                align-items: flex-end;
                flex-wrap: wrap;
            }
            .proxima-card__principal {
                min-width: 0;
                flex: 1 1 18rem;
            }
            .proxima-card__titulo {
                margin: 0 0 0.15rem;
                font-size: 1.5rem;
                font-weight: 700;
                line-height: 1.15;
                color: var(--text-color);
            }
            .proxima-card__codigo {
                font-size: 0.8rem;
                color: var(--text-color-secondary);
                margin-bottom: 0.75rem;
                text-transform: uppercase;
                letter-spacing: 0.04em;
            }
            .proxima-card__meta {
                display: flex;
                flex-direction: column;
                gap: 0.35rem;
                font-size: 0.92rem;
                color: var(--text-color);
            }
            .proxima-card__meta i {
                color: var(--primary-color);
                margin-right: 0.45rem;
                width: 1rem;
                text-align: center;
            }
            .proxima-card__acoes {
                display: flex;
                gap: 0.5rem;
                flex-wrap: wrap;
            }
            .reservas-toolbar {
                display: flex;
                gap: 0.75rem;
                margin-bottom: 1rem;
                flex-wrap: wrap;
                align-items: center;
            }
            .reservas-busca {
                flex: 1 1 14rem;
                max-width: 24rem;
            }
            .reservas-busca input {
                width: 100%;
            }
            .reservas-filtro-status {
                flex: 0 1 18rem;
                min-width: 12rem;
            }
        `
    ]
})
export class MyReservationsPage {
    private readonly api = inject(ReservationService);
    private readonly toast = inject(MessageService);
    private readonly confirm = inject(ConfirmationService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly loading = signal(true);
    readonly reservations = signal<Reservation[]>([]);
    readonly qrUrl = signal<string | null>(null);

    readonly opcoesStatus = RESERVATION_STATUSES.map((status) => ({ label: STATUS_LABEL[status], value: status }));

    searchText = '';
    statusSelecionados: ReservationStatus[] = [...RESERVATION_STATUSES];

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('roles.member') }, { label: this.translate.instant('reservations.myList.title') }];
    });

    readonly proximaReserva = computed<Reservation | null>(() => {
        const candidatos = this.reservations()
            .filter((r) => STATUS_PROXIMAS.includes(r.status))
            .sort((a, b) => +new Date(a.startUtc) - +new Date(b.startUtc));
        const ativa = candidatos.find((r) => r.status === 'InProgress');
        if (ativa) return ativa;
        return candidatos[0] ?? null;
    });

    constructor() {
        this.load();
    }

    filtrar(items: Reservation[]): Reservation[] {
        const statusAtivos = this.statusSelecionados;
        const term = this.searchText.trim().toLowerCase();
        return items.filter((r) => {
            if (statusAtivos.length > 0 && !statusAtivos.includes(r.status)) return false;
            if (!term) return true;
            return r.resourceName.toLowerCase().includes(term) || r.resourceExternalId.toLowerCase().includes(term) || r.pavilionName.toLowerCase().includes(term);
        });
    }

    contagemRegressiva(r: Reservation): string {
        if (r.status === 'InProgress') return this.translate.instant('reservations.myList.countdown.inProgress');
        const ms = new Date(r.startUtc).getTime() - Date.now();
        if (ms <= 0) return this.translate.instant('reservations.myList.countdown.now');
        const min = Math.round(ms / 60000);
        if (min < 60) return this.translate.instant('reservations.myList.countdown.inMinutes', { min });
        const h = Math.floor(min / 60);
        const restoMin = min % 60;
        if (h < 24) {
            if (restoMin) return this.translate.instant('reservations.myList.countdown.inHoursMinutes', { h, min: restoMin });
            return this.translate.instant('reservations.myList.countdown.inHours', { h });
        }
        const d = Math.floor(h / 24);
        if (d === 1) return this.translate.instant('reservations.myList.countdown.inDay');
        return this.translate.instant('reservations.myList.countdown.inDays', { d });
    }

    load(): void {
        this.loading.set(true);
        this.api.listMine().subscribe({
            next: (list) => {
                this.reservations.set(list);
                this.loading.set(false);
            },
            error: () => this.loading.set(false)
        });
    }

    canCancel(r: Reservation): boolean {
        return r.status === 'Pending' || r.status === 'Approved';
    }

    canShowQr(r: Reservation): boolean {
        return r.status === 'Approved' || r.status === 'InProgress';
    }

    podeCancelarAgora(r: Reservation): boolean {
        if (r.status === 'Pending') return true;
        if (r.status !== 'Approved') return false;
        const inicio = new Date(r.startUtc).getTime();
        return inicio - Date.now() >= 2 * 60 * 60 * 1000;
    }

    cancelTooltip(r: Reservation): string {
        if (this.podeCancelarAgora(r)) return this.translate.instant('reservations.myList.cancelReservation');
        return this.translate.instant('reservations.myList.cancelTooltipBlocked');
    }

    confirmCancel(r: Reservation): void {
        if (!this.podeCancelarAgora(r)) return;
        this.confirm.confirm({
            message: this.translate.instant('reservations.myList.confirmCancel.message', { resource: r.resourceName }),
            header: this.translate.instant('reservations.myList.confirmCancel.title'),
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: this.translate.instant('reservations.myList.confirmCancel.acceptLabel'),
            rejectLabel: this.translate.instant('reservations.myList.confirmCancel.rejectLabel'),
            accept: () => {
                this.api.cancel(r.id).subscribe({
                    next: () => {
                        this.toast.add({
                            severity: 'success',
                            summary: this.translate.instant('reservations.toasts.cancelled.summary'),
                            detail: this.translate.instant('reservations.toasts.cancelled.detail')
                        });
                        this.load();
                    },
                    error: (err) =>
                        this.toast.add({
                            severity: 'error',
                            summary: this.translate.instant('common.labels.error'),
                            detail: err?.error?.detail || this.translate.instant('reservations.toasts.errorCancel')
                        })
                });
            }
        });
    }

    openQr(r: Reservation): void {
        this.api.qrCode(r.id).subscribe({
            next: (blob) => this.qrUrl.set(URL.createObjectURL(blob)),
            error: () =>
                this.toast.add({
                    severity: 'error',
                    summary: this.translate.instant('common.labels.error'),
                    detail: this.translate.instant('reservations.myList.qrError')
                })
        });
    }

    closeQr(): void {
        const url = this.qrUrl();
        if (url) URL.revokeObjectURL(url);
        this.qrUrl.set(null);
    }
}
