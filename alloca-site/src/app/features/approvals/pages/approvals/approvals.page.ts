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
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { forkJoin } from 'rxjs';
import { ApprovalsService } from '@features/approvals/services/approvals.service';
import { PavilionService } from '@features/pavilions/services/pavilion.service';
import { LanguageService } from '@core/i18n/language.service';
import { Pavilion } from '@features/pavilions/models/pavilion.model';
import { Reservation } from '@features/reservations/models/reservation.model';
import { ReservationStatusTag } from '@features/reservations/components/reservation-status-tag/reservation-status-tag';
import { PageHero } from '@shared/components/page-hero/page-hero';
import { Loader } from '@shared/components/loader/loader';
import { EmptyState } from '@shared/components/empty-state/empty-state';

@Component({
    selector: 'app-manager-approvals',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TranslatePipe,
        ButtonModule,
        ConfirmDialogModule,
        DialogModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule,
        SelectModule,
        TableModule,
        TextareaModule,
        TooltipModule,
        ReservationStatusTag,
        PageHero,
        Loader,
        EmptyState
    ],
    providers: [ConfirmationService],
    template: `
        <p-confirmDialog />

        <app-page-hero [title]="'reservations.approvals.title' | translate" [description]="'reservations.approvals.subtitle' | translate" [breadcrumb]="breadcrumb()">
            <p-button [label]="'common.actions.refresh' | translate" icon="pi pi-refresh" severity="secondary" [outlined]="true" [loading]="loading()" (onClick)="load()" />
        </app-page-hero>

        <div class="card">
            <div class="approvals-toolbar">
                <p-iconfield iconPosition="left" styleClass="approvals-busca">
                    <p-inputicon><i class="pi pi-search"></i></p-inputicon>
                    <input pInputText type="text" [placeholder]="'reservations.approvals.searchPlaceholder' | translate" [(ngModel)]="searchText" class="w-full" />
                </p-iconfield>
                <p-select [options]="pavilionOptions()" optionLabel="label" optionValue="value" [(ngModel)]="pavilionFilter" appendTo="body" styleClass="w-72" (onChange)="load()" />
            </div>

            @if (selecionados().length > 0) {
                <div class="approvals-batch">
                    <span class="approvals-batch__contador">
                        <i class="pi pi-check-square"></i>
                        {{ 'reservations.approvals.selectedCount' | translate: { count: selecionados().length } }}
                    </span>
                    <div class="approvals-batch__acoes">
                        <p-button [label]="'reservations.approvals.approveSelected' | translate" icon="pi pi-check" severity="success" [loading]="submitting()" (onClick)="confirmApproveBatch()" />
                        <p-button [label]="'reservations.approvals.rejectSelected' | translate" icon="pi pi-times" severity="danger" [outlined]="true" (onClick)="openRejectBatch()" />
                        <p-button [label]="'reservations.approvals.clearSelection' | translate" severity="secondary" [text]="true" (onClick)="limparSelecao()" />
                    </div>
                </div>
            }

            @if (loading()) {
                <app-loader [rotulo]="'reservations.approvals.loading' | translate" />
            } @else if (filtered().length === 0) {
                <app-empty-state icone="pi pi-check-circle" [titulo]="'reservations.approvals.empty' | translate" [descricao]="'reservations.approvals.emptyDescription' | translate" />
            } @else {
                <p-table [value]="filtered()" dataKey="id" [paginator]="true" [rows]="10" [rowsPerPageOptions]="[10, 25, 50]" responsiveLayout="scroll" sortField="startUtc" [sortOrder]="1" [(selection)]="selectionRef" selectionMode="multiple">
                    <ng-template pTemplate="header">
                        <tr>
                            <th style="width: 3rem">
                                <p-tableHeaderCheckbox />
                            </th>
                            <th pSortableColumn="resourceName">{{ 'reservations.approvals.columns.resource' | translate }} <p-sortIcon field="resourceName" /></th>
                            <th pSortableColumn="pavilionName">{{ 'reservations.approvals.columns.pavilion' | translate }} <p-sortIcon field="pavilionName" /></th>
                            <th pSortableColumn="startUtc">{{ 'reservations.approvals.columns.period' | translate }} <p-sortIcon field="startUtc" /></th>
                            <th>{{ 'reservations.approvals.columns.notes' | translate }}</th>
                            <th>{{ 'reservations.approvals.columns.status' | translate }}</th>
                            <th class="text-right">{{ 'reservations.approvals.columns.actions' | translate }}</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-r>
                        <tr>
                            <td>
                                <p-tableCheckbox [value]="r" />
                            </td>
                            <td>
                                <div class="font-medium">{{ r.resourceName }}</div>
                                <div class="text-xs text-muted-color">{{ r.resourceExternalId }}</div>
                            </td>
                            <td>{{ r.pavilionName }}</td>
                            <td>
                                <div>{{ r.startUtc | date: 'dd/MM/yyyy HH:mm' }}</div>
                                <div class="text-xs text-muted-color">{{ 'reservations.approvals.until' | translate }} {{ r.endUtc | date: 'HH:mm' }} · {{ duracao(r) }}</div>
                            </td>
                            <td class="approvals-notes">
                                <span [pTooltip]="r.notes || ''" tooltipPosition="top">
                                    {{ r.notes || '—' }}
                                </span>
                            </td>
                            <td><app-reservation-status-tag [status]="r.status" /></td>
                            <td class="text-right whitespace-nowrap">
                                <p-button
                                    icon="pi pi-check"
                                    severity="success"
                                    [rounded]="true"
                                    [text]="true"
                                    [pTooltip]="'reservations.approvals.approve' | translate"
                                    [ariaLabel]="'reservations.approvals.approve' | translate"
                                    (onClick)="confirmApprove(r)"
                                />
                                <p-button
                                    icon="pi pi-times"
                                    severity="danger"
                                    [rounded]="true"
                                    [text]="true"
                                    [pTooltip]="'reservations.approvals.reject' | translate"
                                    [ariaLabel]="'reservations.approvals.reject' | translate"
                                    (onClick)="openReject(r)"
                                />
                            </td>
                        </tr>
                    </ng-template>
                </p-table>
            }
        </div>

        <p-dialog
            [visible]="!!rejectTarget() || rejectBatchOpen()"
            (visibleChange)="$event || closeReject()"
            [modal]="true"
            [closable]="true"
            [style]="{ width: '32rem' }"
            [header]="(rejectBatchOpen() ? 'reservations.approvals.rejectDialog.titleBatch' : 'reservations.approvals.rejectDialog.titleSingle') | translate"
        >
            @if (rejectBatchOpen()) {
                <div class="flex flex-col gap-3">
                    <div [innerHTML]="'reservations.approvals.batchRejectMessage' | translate: { count: selecionados().length }"></div>
                    <div>
                        <label class="block font-medium mb-2">{{ 'reservations.approvals.reasonLabel' | translate }}</label>
                        <textarea pTextarea rows="4" [(ngModel)]="rejectReason" class="w-full"></textarea>
                    </div>
                </div>
            } @else if (rejectTarget(); as r) {
                <div class="flex flex-col gap-3">
                    <div>
                        <strong>{{ 'reservations.approvals.resource' | translate }}:</strong> {{ r.resourceName }}
                    </div>
                    <div>
                        <label class="block font-medium mb-2">{{ 'reservations.approvals.reasonLabel' | translate }}</label>
                        <textarea pTextarea rows="4" [(ngModel)]="rejectReason" class="w-full"></textarea>
                    </div>
                </div>
            }
            <ng-template pTemplate="footer">
                <p-button [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" (onClick)="closeReject()" />
                <p-button [label]="'reservations.approvals.reject' | translate" severity="danger" icon="pi pi-times" [loading]="submitting()" [disabled]="!rejectReason.trim()" (onClick)="rejectBatchOpen() ? rejectBatch() : reject()" />
            </ng-template>
        </p-dialog>
    `,
    styles: [
        `
            .approvals-toolbar {
                display: flex;
                gap: 0.75rem;
                margin-bottom: 1rem;
                flex-wrap: wrap;
                align-items: center;
            }
            .approvals-busca {
                flex: 1 1 14rem;
                max-width: 24rem;
            }
            .approvals-busca input {
                width: 100%;
            }
            .approvals-batch {
                display: flex;
                justify-content: space-between;
                align-items: center;
                gap: 0.75rem;
                padding: 0.6rem 0.85rem;
                margin-bottom: 0.85rem;
                background: color-mix(in srgb, var(--primary-color), transparent 92%);
                border: 1px solid color-mix(in srgb, var(--primary-color), transparent 80%);
                border-radius: 0.55rem;
                flex-wrap: wrap;
            }
            .approvals-batch__contador {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                font-weight: 600;
                color: var(--primary-color);
            }
            .approvals-batch__acoes {
                display: flex;
                gap: 0.4rem;
                flex-wrap: wrap;
            }
            .approvals-notes {
                max-width: 14rem;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
        `
    ]
})
export class ApprovalsPage {
    private readonly api = inject(ApprovalsService);
    private readonly pavilionsApi = inject(PavilionService);
    private readonly toast = inject(MessageService);
    private readonly confirm = inject(ConfirmationService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly loading = signal(true);
    readonly submitting = signal(false);
    readonly pending = signal<Reservation[]>([]);
    readonly pavilions = signal<Pavilion[]>([]);
    readonly selecionados = signal<Reservation[]>([]);
    readonly rejectBatchOpen = signal(false);

    get selectionRef() {
        return this.selecionados();
    }
    set selectionRef(v: Reservation[]) {
        this.selecionados.set(v);
    }

    pavilionFilter: string | null = null;
    searchText = '';

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('menu.operations') }, { label: this.translate.instant('reservations.approvals.title') }];
    });

    readonly pavilionOptions = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('reservations.approvals.allPavilions'), value: null }, ...this.pavilions().map((p) => ({ label: p.name, value: p.id }))];
    });

    readonly filtered = computed(() => {
        const term = this.searchText.trim().toLowerCase();
        const list = this.pending();
        if (!term) return list;
        return list.filter((r) => r.resourceName.toLowerCase().includes(term) || r.resourceExternalId.toLowerCase().includes(term) || r.pavilionName.toLowerCase().includes(term));
    });

    readonly rejectTarget = signal<Reservation | null>(null);
    rejectReason = '';

    constructor() {
        this.pavilionsApi.list().subscribe((p) => this.pavilions.set(p));
        this.load();
    }

    load(): void {
        this.loading.set(true);
        this.selecionados.set([]);
        this.api.listPending(this.pavilionFilter).subscribe({
            next: (list) => {
                this.pending.set(list);
                this.loading.set(false);
            },
            error: () => this.loading.set(false)
        });
    }

    duracao(r: Reservation): string {
        const ms = new Date(r.endUtc).getTime() - new Date(r.startUtc).getTime();
        const minutos = Math.round(ms / 60000);
        const h = Math.floor(minutos / 60);
        const m = minutos % 60;
        if (h > 0 && m > 0) return `${h}h${m}min`;
        if (h > 0) return `${h}h`;
        return `${m}min`;
    }

    limparSelecao(): void {
        this.selecionados.set([]);
    }

    private err(): string {
        return this.translate.instant('common.labels.error');
    }

    confirmApprove(r: Reservation): void {
        this.confirm.confirm({
            header: this.translate.instant('reservations.approvals.confirmApprove.title'),
            message: this.translate.instant('reservations.approvals.confirmApprove.message', { resource: r.resourceName }),
            icon: 'pi pi-check-circle',
            acceptLabel: this.translate.instant('reservations.approvals.confirmApprove.acceptLabel'),
            rejectLabel: this.translate.instant('common.actions.cancel'),
            accept: () => {
                this.api.approve(r.id).subscribe({
                    next: () => {
                        this.toast.add({
                            severity: 'success',
                            summary: this.translate.instant('reservations.approvals.toasts.approved.summary'),
                            detail: this.translate.instant('reservations.approvals.toasts.approved.detail')
                        });
                        this.load();
                    },
                    error: (err) =>
                        this.toast.add({
                            severity: 'error',
                            summary: this.err(),
                            detail: err?.error?.detail || this.translate.instant('reservations.approvals.toasts.errorApprove')
                        })
                });
            }
        });
    }

    confirmApproveBatch(): void {
        const items = this.selecionados();
        if (items.length === 0) return;
        this.confirm.confirm({
            header: this.translate.instant('reservations.approvals.confirmApproveBatch.title'),
            message: this.translate.instant('reservations.approvals.confirmApproveBatch.message', { count: items.length }),
            icon: 'pi pi-check-circle',
            acceptLabel: this.translate.instant('reservations.approvals.confirmApproveBatch.acceptLabel'),
            rejectLabel: this.translate.instant('common.actions.cancel'),
            accept: () => {
                this.submitting.set(true);
                forkJoin(items.map((r) => this.api.approve(r.id))).subscribe({
                    next: () => {
                        this.submitting.set(false);
                        this.toast.add({
                            severity: 'success',
                            summary: this.translate.instant('reservations.approvals.toasts.approvedBatch.summary'),
                            detail: this.translate.instant('reservations.approvals.toasts.approvedBatch.detail', { count: items.length })
                        });
                        this.load();
                    },
                    error: () => {
                        this.submitting.set(false);
                        this.toast.add({
                            severity: 'error',
                            summary: this.err(),
                            detail: this.translate.instant('reservations.approvals.toasts.errorBatchApprove')
                        });
                        this.load();
                    }
                });
            }
        });
    }

    openReject(r: Reservation): void {
        this.rejectTarget.set(r);
        this.rejectBatchOpen.set(false);
        this.rejectReason = '';
    }

    openRejectBatch(): void {
        if (this.selecionados().length === 0) return;
        this.rejectBatchOpen.set(true);
        this.rejectTarget.set(null);
        this.rejectReason = '';
    }

    closeReject(): void {
        this.rejectTarget.set(null);
        this.rejectBatchOpen.set(false);
    }

    reject(): void {
        const r = this.rejectTarget();
        if (!r || !this.rejectReason.trim()) return;
        this.submitting.set(true);
        this.api.reject(r.id, { reason: this.rejectReason.trim() }).subscribe({
            next: () => {
                this.submitting.set(false);
                this.closeReject();
                this.toast.add({
                    severity: 'success',
                    summary: this.translate.instant('reservations.approvals.toasts.rejected.summary'),
                    detail: this.translate.instant('reservations.approvals.toasts.rejected.detail')
                });
                this.load();
            },
            error: (err) => {
                this.submitting.set(false);
                this.toast.add({
                    severity: 'error',
                    summary: this.err(),
                    detail: err?.error?.detail || this.translate.instant('reservations.approvals.toasts.errorReject')
                });
            }
        });
    }

    rejectBatch(): void {
        const items = this.selecionados();
        const motivo = this.rejectReason.trim();
        if (items.length === 0 || !motivo) return;
        this.submitting.set(true);
        forkJoin(items.map((r) => this.api.reject(r.id, { reason: motivo }))).subscribe({
            next: () => {
                this.submitting.set(false);
                this.closeReject();
                this.toast.add({
                    severity: 'success',
                    summary: this.translate.instant('reservations.approvals.toasts.rejectedBatch.summary'),
                    detail: this.translate.instant('reservations.approvals.toasts.rejectedBatch.detail', { count: items.length })
                });
                this.load();
            },
            error: () => {
                this.submitting.set(false);
                this.closeReject();
                this.toast.add({
                    severity: 'error',
                    summary: this.err(),
                    detail: this.translate.instant('reservations.approvals.toasts.errorBatchReject')
                });
                this.load();
            }
        });
    }
}
