import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { MessageService } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { PavilionService } from '@features/pavilions/services/pavilion.service';
import { ReservationService } from '@features/reservations/services/reservation.service';
import { LanguageService } from '@core/i18n/language.service';
import { AvailabilityResource, Floor, Pavilion, ResourceType } from '@features/pavilions/models/pavilion.model';
import { PageHero } from '@shared/components/page-hero/page-hero';
import { FloorMapComponent } from '@shared/components/floor-map/floor-map.component';

interface TimeOption {
    label: string;
    value: string;
}

interface DurationOption {
    label: string;
    value: number;
}

const RESOURCE_TYPE_KEYS: { value: ResourceType; labelKey: string; icon: string }[] = [
    { value: 'Room', labelKey: 'reservations.reserve.types.room', icon: 'pi pi-building' },
    { value: 'Desk', labelKey: 'reservations.reserve.types.desk', icon: 'pi pi-desktop' }
];

const DURATION_BASE: { value: number; labelKey: string }[] = [
    { value: 30, labelKey: 'reservations.reserve.durations.min30' },
    { value: 60, labelKey: 'reservations.reserve.durations.h1' },
    { value: 90, labelKey: 'reservations.reserve.durations.h1m30' },
    { value: 120, labelKey: 'reservations.reserve.durations.h2' }
];

@Component({
    selector: 'app-reserve',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        TranslatePipe,
        ButtonModule,
        CardModule,
        DatePickerModule,
        DialogModule,
        MessageModule,
        ProgressSpinnerModule,
        SelectModule,
        SelectButtonModule,
        TextareaModule,
        ToastModule,
        TooltipModule,
        PageHero,
        FloorMapComponent
    ],
    providers: [MessageService],
    template: `
        <p-toast />

        <app-page-hero [title]="'reservations.reserve.title' | translate" [description]="'reservations.reserve.subtitle' | translate" [breadcrumb]="breadcrumb()">
            <p-button [label]="'reservations.reserve.myReservations' | translate" icon="pi pi-list" severity="secondary" [outlined]="true" routerLink="/student/reservations" />
        </app-page-hero>

        <div class="card reserva-card">
            <!-- Linha de filtros -->
            <div class="reserva-filtros">
                <div class="reserva-campo">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-building"></i> {{ 'reservations.reserve.pavilionLabel' | translate }} </label>
                    <p-select [options]="pavilions()" optionLabel="name" optionValue="id" [(ngModel)]="pavilionId" [placeholder]="'reservations.reserve.selectPlaceholder' | translate" appendTo="body" styleClass="w-full" />
                </div>

                <div class="reserva-campo">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-th-large"></i> {{ 'reservations.reserve.floorLabel' | translate }} </label>
                    <p-select
                        [options]="floors()"
                        optionLabel="name"
                        optionValue="id"
                        [(ngModel)]="floorId"
                        (ngModelChange)="onFloorChange()"
                        [placeholder]="'reservations.reserve.selectPlaceholder' | translate"
                        appendTo="body"
                        styleClass="w-full"
                        [disabled]="!pavilionId() || floors().length === 0"
                    />
                </div>

                <div class="reserva-campo">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-calendar"></i> {{ 'reservations.reserve.dateLabel' | translate }} </label>
                    <p-datepicker [(ngModel)]="date" dateFormat="dd/mm/yy" [showIcon]="true" [minDate]="hoje" appendTo="body" styleClass="w-full" />
                </div>

                <div class="reserva-campo">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-clock"></i> {{ 'reservations.reserve.startLabel' | translate }} </label>
                    <p-select
                        [options]="startOptions()"
                        optionLabel="label"
                        optionValue="value"
                        [(ngModel)]="startTime"
                        [placeholder]="'reservations.reserve.hourPlaceholder' | translate"
                        appendTo="body"
                        styleClass="w-full"
                        [disabled]="startOptions().length === 0"
                    />
                </div>

                <div class="reserva-campo reserva-campo--tipo">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-tag"></i> {{ 'reservations.reserve.typeLabel' | translate }} </label>
                    <p-select [options]="resourceTypeOptions()" optionLabel="label" optionValue="value" [(ngModel)]="resourceType" appendTo="body" styleClass="w-full" />
                </div>

                <div class="reserva-campo reserva-campo--duracao">
                    <label class="reserva-campo__rotulo"> <i class="pi pi-stopwatch"></i> {{ 'reservations.reserve.durationLabel' | translate }} </label>
                    <p-selectbutton [options]="durationOptions()" optionLabel="label" optionValue="value" [(ngModel)]="durationMinutes" [allowEmpty]="false" [ariaLabel]="'reservations.reserve.durationLabel' | translate" fluid />
                </div>
            </div>

            <!-- Hint de horário de funcionamento / avisos -->
            @if (operatingHoursLabel(); as label) {
                <div class="reserva-hint">
                    <i class="pi pi-info-circle"></i>
                    <span>{{ label }}</span>
                    @if (formattedWindow(); as w) {
                        <span class="reserva-hint__sep">·</span>
                        <span class="reserva-hint__forte">{{ w }}</span>
                    }
                </div>
            } @else if (selectedPavilion() && date() && !hasOperatingHoursData()) {
                <p-message severity="warn" [text]="'reservations.reserve.noOperatingHours' | translate" styleClass="w-full mt-2" />
            } @else if (selectedPavilion() && date()) {
                <p-message severity="warn" [text]="'reservations.reserve.closedToday' | translate" styleClass="w-full mt-2" />
            }
        </div>

        <!-- Resultado: mapa interativo -->
        @if (buscou()) {
            <div class="card reserva-mapa-card mt-4">
                <header class="reserva-mapa__header">
                    <div>
                        <div class="reserva-mapa__titulo">
                            <i class="pi pi-map mr-2"></i>
                            {{ selectedFloor()?.name }} · {{ selectedPavilion()?.name }}
                        </div>
                        <div class="reserva-mapa__subtitulo">
                            {{ 'reservations.reserve.map.clickAvailable' | translate }}
                        </div>
                    </div>
                    <div class="reserva-mapa__legenda">
                        <span class="legenda legenda--ok">
                            <span class="legenda__bola"></span> {{ 'reservations.reserve.map.available' | translate }}
                            <strong>{{ availableCount() }}</strong>
                        </span>
                        <span class="legenda legenda--off">
                            <span class="legenda__bola"></span> {{ 'reservations.reserve.map.unavailable' | translate }}
                            <strong>{{ filteredResources().length - availableCount() }}</strong>
                        </span>
                    </div>
                </header>

                <div class="reserva-mapa__conteudo">
                    @if (loadingAvailability()) {
                        <div class="reserva-mapa-overlay">
                            <p-progressSpinner strokeWidth="4" />
                        </div>
                    }
                    @if (floorSvgUrl(); as url) {
                        <app-floor-map [svgUrl]="url" [ariaLabel]="mapAriaLabel()" [recursos]="filteredResources()" (recursoSelecionado)="openConfirm($event)" (recursoIndisponivelSelecionado)="avisarRecursoIndisponivel($event)" class="block" />
                    }
                </div>

                @if (!loadingAvailability() && filteredResources().length === 0) {
                    <p-message severity="info" [text]="'reservations.reserve.map.emptyResources' | translate" styleClass="w-full mt-3" />
                }
            </div>
        } @else {
            <div class="card reserva-vazio mt-4">
                <i class="pi pi-search reserva-vazio__icone"></i>
                <div class="reserva-vazio__titulo">{{ 'reservations.reserve.empty.title' | translate }}</div>
                <div class="reserva-vazio__subtitulo">
                    {{ 'reservations.reserve.empty.description' | translate }}
                </div>
            </div>
        }

        <p-dialog [visible]="!!selected()" (visibleChange)="$event || cancelConfirm()" [modal]="true" [closable]="true" [draggable]="false" [dismissableMask]="true" [style]="{ width: '36rem' }" styleClass="reserva-dialog">
            <ng-template pTemplate="header">
                @if (selected(); as r) {
                    <div class="reserva-dialog__header">
                        <div class="reserva-dialog__icon">
                            <i [class]="r.type === 'Room' ? 'pi pi-building' : 'pi pi-desktop'"></i>
                        </div>
                        <div>
                            <div class="reserva-dialog__eyebrow">
                                {{ (r.type === 'Room' ? 'reservations.reserve.dialog.eyebrowRoom' : 'reservations.reserve.dialog.eyebrowDesk') | translate }}
                            </div>
                            <h2 class="reserva-dialog__title">{{ 'reservations.reserve.dialog.title' | translate }}</h2>
                        </div>
                    </div>
                }
            </ng-template>

            @if (selected(); as r) {
                <div class="reserva-dialog__body">
                    <section class="reserva-resumo">
                        <div class="reserva-resumo__nome">{{ r.name }}</div>
                        <div class="reserva-resumo__codigo">
                            <i class="pi pi-tag"></i>
                            <span>{{ r.externalId }}</span>
                        </div>
                        <div class="reserva-resumo__local">
                            <i class="pi pi-map-marker"></i>
                            <span>{{ selectedPavilion()?.name }} · {{ selectedFloor()?.name }}</span>
                        </div>
                    </section>

                    <section class="reserva-grade">
                        <div class="reserva-grade__item">
                            <span class="reserva-grade__rotulo"> <i class="pi pi-calendar"></i> {{ 'reservations.reserve.dialog.date' | translate }} </span>
                            <span class="reserva-grade__valor">{{ dataFormatada() }}</span>
                        </div>
                        <div class="reserva-grade__item">
                            <span class="reserva-grade__rotulo"> <i class="pi pi-clock"></i> {{ 'reservations.reserve.dialog.time' | translate }} </span>
                            <span class="reserva-grade__valor"> {{ startTime() }} – {{ endTime() }} </span>
                        </div>
                        <div class="reserva-grade__item">
                            <span class="reserva-grade__rotulo"> <i class="pi pi-stopwatch"></i> {{ 'reservations.reserve.dialog.duration' | translate }} </span>
                            <span class="reserva-grade__valor">{{ duracaoFormatada() }}</span>
                        </div>
                    </section>

                    <section class="reserva-avisos">
                        <div class="reserva-aviso reserva-aviso--info">
                            <i class="pi pi-info-circle"></i>
                            <div>
                                <strong>{{ 'reservations.reserve.dialog.managerApproval.title' | translate }}</strong>
                                <p>{{ 'reservations.reserve.dialog.managerApproval.description' | translate }}</p>
                            </div>
                        </div>
                        <div class="reserva-aviso reserva-aviso--warn">
                            <i class="pi pi-qrcode"></i>
                            <div>
                                <strong>{{ 'reservations.reserve.dialog.checkIn.title' | translate }}</strong>
                                <p>{{ 'reservations.reserve.dialog.checkIn.description' | translate }}</p>
                            </div>
                        </div>
                        <div class="reserva-aviso reserva-aviso--muted">
                            <i class="pi pi-ban"></i>
                            <div>
                                <strong>{{ 'reservations.reserve.dialog.cancellation.title' | translate }}</strong>
                                <p>{{ 'reservations.reserve.dialog.cancellation.description' | translate }}</p>
                            </div>
                        </div>
                    </section>

                    <section class="reserva-observacoes">
                        <div class="reserva-observacoes__cabecalho">
                            <label for="reserva-notes" class="reserva-observacoes__rotulo">
                                {{ 'reservations.reserve.dialog.notesLabel' | translate }} <span class="reserva-observacoes__opcional">{{ 'reservations.reserve.dialog.notesOptional' | translate }}</span>
                            </label>
                            <span class="reserva-observacoes__contador">{{ notes.length }}/250</span>
                        </div>
                        <textarea id="reserva-notes" pTextarea rows="3" maxlength="250" [(ngModel)]="notes" [placeholder]="'reservations.reserve.dialog.notesPlaceholder' | translate" class="w-full"></textarea>
                    </section>
                </div>
            }

            <ng-template pTemplate="footer">
                <div class="reserva-dialog__footer">
                    <p-button [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" (onClick)="cancelConfirm()" />
                    <p-button [label]="'reservations.reserve.dialog.confirm' | translate" icon="pi pi-check" [loading]="submitting()" (onClick)="submit()" />
                </div>
            </ng-template>
        </p-dialog>
    `,
    styles: [
        `
            /* === Card unificado de filtros === */
            .reserva-card {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }

            .reserva-filtros {
                display: grid;
                grid-template-columns: repeat(12, 1fr);
                gap: 0.75rem 1rem;
            }
            .reserva-campo {
                display: flex;
                flex-direction: column;
                gap: 0.35rem;
                grid-column: span 12;
            }
            @media (min-width: 768px) {
                .reserva-campo {
                    grid-column: span 6;
                }
                .reserva-campo--tipo {
                    grid-column: span 4;
                }
                .reserva-campo--duracao {
                    grid-column: span 3;
                }
            }
            @media (min-width: 1100px) {
                .reserva-campo {
                    grid-column: span 3;
                }
                .reserva-campo--tipo {
                    grid-column: span 3;
                }
                .reserva-campo--duracao {
                    grid-column: span 3;
                }
            }
            .reserva-campo__rotulo {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                font-size: 0.78rem;
                font-weight: 600;
                color: var(--text-color-secondary);
            }
            .reserva-campo__rotulo i {
                font-size: 0.85rem;
                color: var(--primary-color);
            }
            :host ::ng-deep .reserva-card .p-datepicker-input,
            :host ::ng-deep .reserva-card .p-select {
                width: 100%;
            }
            :host ::ng-deep .reserva-campo--duracao .p-selectbutton {
                display: flex;
                flex-wrap: wrap;
            }
            :host ::ng-deep .reserva-campo--duracao .p-selectbutton .p-button {
                flex: 1 1 5rem;
                min-width: 5rem;
            }

            .reserva-hint {
                display: inline-flex;
                align-items: center;
                gap: 0.5rem;
                padding: 0.5rem 0.85rem;
                font-size: 0.8rem;
                border-radius: 0.5rem;
                background: color-mix(in srgb, var(--primary-color), transparent 92%);
                color: var(--primary-color);
                align-self: flex-start;
            }
            .reserva-hint__sep {
                opacity: 0.5;
            }
            .reserva-hint__forte {
                font-weight: 600;
            }

            /* === Card do mapa === */
            .reserva-mapa-card {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }
            .reserva-mapa__conteudo {
                position: relative;
            }
            .reserva-mapa-overlay {
                position: absolute;
                inset: 0;
                z-index: 10;
                display: flex;
                align-items: center;
                justify-content: center;
                background: color-mix(in srgb, var(--surface-0), transparent 25%);
                border-radius: 0.5rem;
                backdrop-filter: blur(1px);
            }
            :host ::ng-deep .app-dark .reserva-mapa-overlay {
                background: color-mix(in srgb, var(--surface-900), transparent 25%);
            }
            .reserva-mapa__header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                flex-wrap: wrap;
                gap: 0.75rem;
            }
            .reserva-mapa__titulo {
                font-size: 1.05rem;
                font-weight: 600;
                display: inline-flex;
                align-items: center;
            }
            .reserva-mapa__titulo i {
                color: var(--primary-color);
            }
            .reserva-mapa__subtitulo {
                font-size: 0.8rem;
                color: var(--text-color-secondary);
            }
            .reserva-mapa__legenda {
                display: flex;
                gap: 0.5rem;
                flex-wrap: wrap;
            }
            .legenda {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                padding: 0.3rem 0.7rem;
                font-size: 0.75rem;
                border-radius: 999px;
                background: var(--surface-100);
                color: var(--text-color-secondary);
            }
            .legenda strong {
                color: var(--text-color);
                margin-left: 0.15rem;
            }
            .legenda__bola {
                width: 0.6rem;
                height: 0.6rem;
                border-radius: 999px;
                display: inline-block;
            }
            .legenda--ok .legenda__bola {
                background: var(--p-green-400);
            }
            .legenda--off .legenda__bola {
                background: var(--p-red-300);
            }
            :host ::ng-deep .app-dark .legenda {
                background: var(--surface-800);
            }

            /* === Estado vazio === */
            .reserva-vazio {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 0.4rem;
                padding: 2.5rem 1rem;
                text-align: center;
                border: 1px dashed var(--surface-border);
                background: var(--surface-50);
            }
            .reserva-vazio__icone {
                font-size: 1.75rem;
                color: var(--text-color-secondary);
                margin-bottom: 0.4rem;
            }
            .reserva-vazio__titulo {
                font-weight: 600;
                font-size: 1rem;
            }
            .reserva-vazio__subtitulo {
                font-size: 0.85rem;
                color: var(--text-color-secondary);
                max-width: 32rem;
            }
            :host ::ng-deep .app-dark .reserva-vazio {
                background: var(--surface-900);
            }

            /* === Modal de reserva === */
            :host ::ng-deep .reserva-dialog .p-dialog-header {
                padding: 1.25rem 1.5rem 0.75rem;
                border-bottom: 1px solid var(--surface-border);
            }
            :host ::ng-deep .reserva-dialog .p-dialog-content {
                padding: 1.25rem 1.5rem;
            }
            :host ::ng-deep .reserva-dialog .p-dialog-footer {
                padding: 0.75rem 1.5rem 1.25rem;
                border-top: 1px solid var(--surface-border);
            }

            .reserva-dialog__header {
                display: flex;
                align-items: center;
                gap: 0.85rem;
            }
            .reserva-dialog__icon {
                width: 2.75rem;
                height: 2.75rem;
                border-radius: 999px;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                background: color-mix(in srgb, var(--primary-color), transparent 88%);
                color: var(--primary-color);
                font-size: 1.25rem;
            }
            .reserva-dialog__eyebrow {
                font-size: 0.72rem;
                letter-spacing: 0.06em;
                text-transform: uppercase;
                color: var(--text-color-secondary);
                font-weight: 600;
            }
            .reserva-dialog__title {
                margin: 0.1rem 0 0;
                font-size: 1.15rem;
                font-weight: 600;
                line-height: 1.2;
            }

            .reserva-dialog__body {
                display: flex;
                flex-direction: column;
                gap: 1.1rem;
            }

            .reserva-resumo {
                display: flex;
                flex-direction: column;
                gap: 0.35rem;
                padding: 0.95rem 1rem;
                border-radius: 0.6rem;
                background: color-mix(in srgb, var(--primary-color), transparent 94%);
                border: 1px solid color-mix(in srgb, var(--primary-color), transparent 80%);
            }
            .reserva-resumo__nome {
                font-size: 1.05rem;
                font-weight: 600;
            }
            .reserva-resumo__codigo,
            .reserva-resumo__local {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                font-size: 0.8rem;
                color: var(--text-color-secondary);
            }
            .reserva-resumo__codigo i,
            .reserva-resumo__local i {
                font-size: 0.85rem;
            }

            .reserva-grade {
                display: grid;
                grid-template-columns: repeat(3, 1fr);
                gap: 0.5rem;
            }
            .reserva-grade__item {
                display: flex;
                flex-direction: column;
                gap: 0.25rem;
                padding: 0.7rem 0.85rem;
                border: 1px solid var(--surface-border);
                border-radius: 0.55rem;
                background: var(--surface-0);
            }
            .reserva-grade__rotulo {
                display: inline-flex;
                align-items: center;
                gap: 0.35rem;
                font-size: 0.72rem;
                text-transform: uppercase;
                letter-spacing: 0.04em;
                color: var(--text-color-secondary);
                font-weight: 600;
            }
            .reserva-grade__valor {
                font-size: 0.95rem;
                font-weight: 600;
                color: var(--text-color);
            }
            :host ::ng-deep .app-dark .reserva-grade__item {
                background: var(--surface-900);
            }

            .reserva-avisos {
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
            }
            .reserva-aviso {
                display: flex;
                gap: 0.65rem;
                padding: 0.65rem 0.8rem;
                border-radius: 0.5rem;
                font-size: 0.82rem;
                line-height: 1.35;
                border: 1px solid transparent;
            }
            .reserva-aviso i {
                margin-top: 0.15rem;
                font-size: 0.95rem;
                flex-shrink: 0;
            }
            .reserva-aviso strong {
                display: block;
                font-size: 0.83rem;
                margin-bottom: 0.1rem;
            }
            .reserva-aviso p {
                margin: 0;
                color: var(--text-color-secondary);
                font-size: 0.78rem;
            }
            .reserva-aviso--info {
                background: color-mix(in srgb, var(--p-blue-500), transparent 92%);
                border-color: color-mix(in srgb, var(--p-blue-500), transparent 80%);
                color: var(--p-blue-700, var(--p-blue-600));
            }
            .reserva-aviso--warn {
                background: color-mix(in srgb, var(--p-yellow-500), transparent 90%);
                border-color: color-mix(in srgb, var(--p-yellow-500), transparent 75%);
                color: var(--p-yellow-700, #8a6d00);
            }
            .reserva-aviso--muted {
                background: var(--surface-50);
                border-color: var(--surface-border);
                color: var(--text-color);
            }
            :host ::ng-deep .app-dark .reserva-aviso--muted {
                background: var(--surface-800);
            }

            .reserva-observacoes {
                display: flex;
                flex-direction: column;
                gap: 0.4rem;
            }
            .reserva-observacoes__cabecalho {
                display: flex;
                justify-content: space-between;
                align-items: baseline;
            }
            .reserva-observacoes__rotulo {
                font-size: 0.85rem;
                font-weight: 600;
            }
            .reserva-observacoes__opcional {
                font-weight: 400;
                color: var(--text-color-secondary);
                font-size: 0.78rem;
            }
            .reserva-observacoes__contador {
                font-size: 0.72rem;
                color: var(--text-color-secondary);
                font-variant-numeric: tabular-nums;
            }

            .reserva-dialog__footer {
                display: flex;
                justify-content: flex-end;
                gap: 0.5rem;
            }

            @media (max-width: 540px) {
                .reserva-grade {
                    grid-template-columns: 1fr;
                }
            }
        `
    ]
})
export class ReservePage {
    private readonly pavilionsApi = inject(PavilionService);
    private readonly reservations = inject(ReservationService);
    private readonly toast = inject(MessageService);
    private readonly router = inject(Router);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly hoje = new Date();

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('roles.member') }, { label: this.translate.instant('reservations.reserve.title') }];
    });

    readonly resourceTypeOptions = computed(() => {
        void this.language.atual();
        return RESOURCE_TYPE_KEYS.map((o) => ({
            label: this.translate.instant(o.labelKey),
            value: o.value,
            icon: o.icon
        }));
    });

    readonly mapAriaLabel = computed(() => {
        void this.language.atual();
        return this.translate.instant('reservations.reserve.map.ariaLabel', {
            name: this.selectedFloor()?.name ?? ''
        });
    });

    readonly pavilions = signal<Pavilion[]>([]);
    readonly floors = signal<Floor[]>([]);
    readonly resources = signal<AvailabilityResource[]>([]);
    readonly loadingAvailability = signal(false);
    readonly submitting = signal(false);
    readonly selected = signal<AvailabilityResource | null>(null);
    readonly buscou = signal(false);

    readonly pavilionId = model<string | null>(null);
    readonly floorId = model<string | null>(null);
    readonly date = model<Date>(new Date());
    readonly startTime = model<string | null>(null);
    readonly durationMinutes = model<number>(60);
    readonly resourceType = model<ResourceType>('Room');
    notes = '';

    readonly selectedPavilion = computed(() => this.pavilions().find((p) => p.id === this.pavilionId()) ?? null);

    readonly selectedFloor = computed(() => this.floors().find((f) => f.id === this.floorId()) ?? null);

    readonly floorSvgUrl = computed(() => {
        const key = this.selectedFloor()?.svgKey;
        return key ? `/floors/${key}.svg` : null;
    });

    readonly hasOperatingHoursData = computed(() => {
        const p = this.selectedPavilion();
        return !!p && Array.isArray(p.operatingHours) && p.operatingHours.length > 0;
    });

    private readonly slotsForDay = computed<string[]>(() => {
        const p = this.selectedPavilion();
        const d = this.date();
        if (!p || !d) return [];
        const oh = (p.operatingHours ?? []).find((o) => o.dayOfWeek === d.getDay());
        if (!oh) return [];
        return this.buildSlots(oh.opensAt, oh.closesAt, p.slotMinutes);
    });

    readonly startOptions = computed<TimeOption[]>(() => {
        const slots = this.slotsForDay();
        if (slots.length < 2) return [];
        const candidatos = slots.slice(0, -1);
        const d = this.date();
        const agora = new Date();
        const ehHoje = !!d && d.getFullYear() === agora.getFullYear() && d.getMonth() === agora.getMonth() && d.getDate() === agora.getDate();
        if (!ehHoje) return candidatos.map((t) => ({ label: t, value: t }));
        const minutosAgora = agora.getHours() * 60 + agora.getMinutes();
        return candidatos.filter((t) => (this.toMinutes(t) ?? -1) >= minutosAgora).map((t) => ({ label: t, value: t }));
    });

    readonly endTime = computed<string | null>(() => {
        const start = this.startTime();
        const dur = this.durationMinutes();
        const slots = this.slotsForDay();
        if (!start || !dur || slots.length === 0) return null;
        const startMin = this.toMinutes(start);
        if (startMin === null) return null;
        const endMin = startMin + dur;
        const last = this.toMinutes(slots[slots.length - 1])!;
        if (endMin > last) return null;
        return this.fromMinutes(endMin);
    });

    readonly durationOptions = computed<DurationOption[]>(() => {
        void this.language.atual();
        const base: DurationOption[] = DURATION_BASE.map((d) => ({
            value: d.value,
            label: this.translate.instant(d.labelKey)
        }));
        const start = this.startTime();
        const slots = this.slotsForDay();
        if (!start || slots.length === 0) return base;
        const startMin = this.toMinutes(start);
        const last = this.toMinutes(slots[slots.length - 1]);
        if (startMin === null || last === null) return base;
        const max = last - startMin;
        return base.filter((o) => o.value <= max);
    });

    readonly operatingHoursLabel = computed(() => {
        void this.language.atual();
        const p = this.selectedPavilion();
        const d = this.date();
        if (!p || !d) return '';
        const oh = (p.operatingHours ?? []).find((o) => o.dayOfWeek === d.getDay());
        if (!oh) return '';
        return this.translate.instant('reservations.reserve.operatingHours', {
            open: oh.opensAt,
            close: oh.closesAt
        });
    });

    readonly filteredResources = computed(() => this.resources().filter((r) => r.type === this.resourceType()));

    readonly availableCount = computed(() => this.filteredResources().filter((r) => r.available).length);

    readonly formattedWindow = computed(() => {
        const s = this.composeDate(this.date(), this.startTime());
        const e = this.composeDate(this.date(), this.endTime());
        if (!s || !e) return '';
        const fmt = new Intl.DateTimeFormat(this.language.atual(), { timeStyle: 'short' });
        return `${fmt.format(s)} – ${fmt.format(e)}`;
    });

    readonly dataFormatada = computed(() => {
        const d = this.date();
        if (!d) return '';
        return new Intl.DateTimeFormat(this.language.atual(), {
            weekday: 'long',
            day: '2-digit',
            month: 'long',
            year: 'numeric'
        }).format(d);
    });

    readonly duracaoFormatada = computed(() => {
        void this.language.atual();
        const minutos = this.durationMinutes() ?? 0;
        const h = Math.floor(minutos / 60);
        const m = minutos % 60;
        if (h > 0 && m > 0) return this.translate.instant('reservations.reserve.durationFormat.hoursMinutes', { h, m });
        if (h > 0) return this.translate.instant('reservations.reserve.durationFormat.hours', { h });
        return this.translate.instant('reservations.reserve.durationFormat.minutes', { m });
    });

    private static readonly STORAGE_ULTIMA = 'reserve:ultima';
    private static readonly STORAGE_ULTIMA_LEGADO = 'alloca.reserve.last';

    constructor() {
        this.pavilionsApi.list().subscribe((p) => {
            this.pavilions.set(p);
            const restaurada = this.restaurarUltimaSelecao(p);

            if (!restaurada && p.length === 1) this.pavilionId.set(p[0].id);
        });

        effect(() => {
            const id = this.pavilionId();
            this.floorId.set(null);
            this.floors.set([]);
            this.resources.set([]);
            this.buscou.set(false);
            if (id) {
                this.pavilionsApi.listFloors(id).subscribe((f) => {
                    this.floors.set(f);
                    if (f.length === 1) this.floorId.set(f[0].id);
                });
            }
        });

        effect(() => {
            const p = this.selectedPavilion();
            if (!p) return;
            const dataAtual = this.date();
            const agora = new Date();
            const ehHoje = dataAtual.getFullYear() === agora.getFullYear() && dataAtual.getMonth() === agora.getMonth() && dataAtual.getDate() === agora.getDate();
            if (!ehHoje) return;
            const proxima = this.calcProximaDataDisponivel(p, agora);
            const hojeZerado = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate()).getTime();
            if (proxima.getTime() !== hojeZerado) {
                this.date.set(proxima);
            }
        });

        effect(() => {
            const opcoes = this.startOptions().map((o) => o.value);
            const start = this.startTime();
            if (start && !opcoes.includes(start)) {
                this.startTime.set(null);
            }
        });

        let buscaTimer: ReturnType<typeof setTimeout> | null = null;
        effect(() => {
            const ok = this.canSearch();
            void this.pavilionId();
            void this.floorId();
            void this.date();
            void this.startTime();
            void this.durationMinutes();
            void this.resourceType();
            if (buscaTimer) clearTimeout(buscaTimer);
            if (!ok) return;
            buscaTimer = setTimeout(() => this.searchAvailability(), 250);
        });
    }

    onFloorChange(): void {
        this.resources.set([]);
        this.buscou.set(false);
    }

    canSearch(): boolean {
        if (!this.pavilionId() || !this.floorId()) return false;
        if (!this.date() || !this.startTime() || !this.endTime()) return false;
        return true;
    }

    searchAvailability(): void {
        const pid = this.pavilionId();
        const fid = this.floorId();
        if (!this.canSearch() || !pid || !fid) return;
        const start = this.composeDate(this.date(), this.startTime());
        const end = this.composeDate(this.date(), this.endTime());
        if (!start || !end) return;
        this.loadingAvailability.set(true);
        this.pavilionsApi.checkAvailability(pid, fid, { startUtc: start.toISOString(), endUtc: end.toISOString() }).subscribe({
            next: (list) => {
                this.resources.set(list);
                this.buscou.set(true);
                this.loadingAvailability.set(false);
            },
            error: () => {
                this.loadingAvailability.set(false);
                this.toast.add({
                    severity: 'error',
                    summary: this.translate.instant('common.labels.error'),
                    detail: this.translate.instant('reservations.reserve.toasts.errorAvailability')
                });
            }
        });
    }

    openConfirm(r: any): void {
        if (!r.available) {
            this.avisarRecursoIndisponivel(r);
            return;
        }
        this.selected.set(r);
        this.notes = '';
    }

    avisarRecursoIndisponivel(r: { name: string; externalId: string }): void {
        this.toast.add({
            severity: 'warn',
            summary: this.translate.instant('reservations.reserve.toasts.unavailableSelected.summary'),
            detail: this.translate.instant('reservations.reserve.toasts.unavailableSelected.detail', {
                name: r.name,
                code: r.externalId
            })
        });
    }

    cancelConfirm(): void {
        this.selected.set(null);
    }

    submit(): void {
        const r = this.selected();
        if (!r) return;
        const start = this.composeDate(this.date(), this.startTime());
        const end = this.composeDate(this.date(), this.endTime());
        if (!start || !end) return;
        this.submitting.set(true);
        this.reservations
            .create({
                resourceType: r.type,
                resourceId: r.id,
                startUtc: start.toISOString(),
                endUtc: end.toISOString(),
                notes: this.notes || null
            })
            .subscribe({
                next: () => {
                    this.submitting.set(false);
                    this.selected.set(null);
                    this.persistirUltimaSelecao();
                    this.toast.add({
                        severity: 'success',
                        summary: this.translate.instant('reservations.reserve.toasts.created.summary'),
                        detail: this.translate.instant('reservations.reserve.toasts.created.detail')
                    });
                    setTimeout(() => this.router.navigate(['/student/reservations']), 600);
                },
                error: (err) => {
                    this.submitting.set(false);
                    const msg = err?.error?.detail || err?.error?.message || this.translate.instant('reservations.reserve.toasts.errorCreate');
                    this.toast.add({
                        severity: 'error',
                        summary: this.translate.instant('common.labels.error'),
                        detail: msg
                    });
                }
            });
    }

    private persistirUltimaSelecao(): void {
        try {
            const dados = {
                pavilionId: this.pavilionId(),
                floorId: this.floorId(),
                resourceType: this.resourceType()
            };
            localStorage.setItem(ReservePage.STORAGE_ULTIMA, JSON.stringify(dados));
        } catch {}
    }

    private restaurarUltimaSelecao(pavilions: { id: string }[]): boolean {
        try {
            let raw = localStorage.getItem(ReservePage.STORAGE_ULTIMA);
            if (!raw) {
                const legado = localStorage.getItem(ReservePage.STORAGE_ULTIMA_LEGADO);
                if (legado) {
                    localStorage.setItem(ReservePage.STORAGE_ULTIMA, legado);
                    localStorage.removeItem(ReservePage.STORAGE_ULTIMA_LEGADO);
                    raw = legado;
                }
            }
            if (!raw) return false;
            const dados = JSON.parse(raw) as {
                pavilionId?: string | null;
                floorId?: string | null;
                resourceType?: ResourceType;
            };
            if (dados.resourceType) this.resourceType.set(dados.resourceType);
            if (dados.pavilionId && pavilions.some((p) => p.id === dados.pavilionId)) {
                this.pavilionId.set(dados.pavilionId);
                if (dados.floorId) {
                    let tentativas = 0;
                    const tentar = () => {
                        const fs = this.floors();
                        if (fs.some((f) => f.id === dados.floorId)) {
                            this.floorId.set(dados.floorId!);
                        } else if (++tentativas < 20) {
                            setTimeout(tentar, 80);
                        }
                    };
                    queueMicrotask(tentar);
                }
                return true;
            }
        } catch {}
        return false;
    }

    private buildSlots(opensAt: string, closesAt: string, slotMinutes: number): string[] {
        const open = this.toMinutes(opensAt);
        const close = this.toMinutes(closesAt);
        if (open === null || close === null || close <= open || slotMinutes <= 0) return [];
        const out: string[] = [];
        for (let m = open; m <= close; m += slotMinutes) {
            out.push(this.fromMinutes(m));
        }

        if (out[out.length - 1] !== this.fromMinutes(close)) {
            out.push(this.fromMinutes(close));
        }
        return out;
    }

    private calcProximaDataDisponivel(pavilion: Pavilion, from: Date, maxDias = 15): Date {
        const agora = new Date();
        const minutosAgora = agora.getHours() * 60 + agora.getMinutes();
        for (let i = 0; i < maxDias; i++) {
            const candidata = new Date(from);
            candidata.setDate(candidata.getDate() + i);
            candidata.setHours(0, 0, 0, 0);
            const oh = (pavilion.operatingHours ?? []).find((o) => o.dayOfWeek === candidata.getDay());
            if (!oh) continue;
            const slots = this.buildSlots(oh.opensAt, oh.closesAt, pavilion.slotMinutes);
            if (slots.length < 2) continue;
            const candidatos = slots.slice(0, -1);
            if (i === 0) {
                const slotsDisponiveis = candidatos.filter((t) => (this.toMinutes(t) ?? -1) >= minutosAgora);
                if (slotsDisponiveis.length === 0) continue;
            }
            return candidata;
        }
        const fallback = new Date(from);
        fallback.setHours(0, 0, 0, 0);
        return fallback;
    }

    private toMinutes(hhmm: string): number | null {
        const m = /^(\d{2}):(\d{2})$/.exec(hhmm);
        if (!m) return null;
        return Number(m[1]) * 60 + Number(m[2]);
    }

    private fromMinutes(total: number): string {
        const h = Math.floor(total / 60)
            .toString()
            .padStart(2, '0');
        const m = (total % 60).toString().padStart(2, '0');
        return `${h}:${m}`;
    }

    private composeDate(date: Date | null, hhmm: string | null): Date | null {
        if (!date || !hhmm) return null;
        const m = /^(\d{2}):(\d{2})$/.exec(hhmm);
        if (!m) return null;
        const d = new Date(date);
        d.setHours(Number(m[1]), Number(m[2]), 0, 0);
        return d;
    }
}
