import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { ApprovalsService } from '@features/approvals/services/approvals.service';
import { PavilionService } from '@features/pavilions/services/pavilion.service';
import { AuthService } from '@core/auth/auth.service';
import { LanguageService } from '@core/i18n/language.service';
import { Reservation } from '@features/reservations/models/reservation.model';
import { PageHero } from '@shared/components/page-hero/page-hero';
import { EmptyState } from '@shared/components/empty-state/empty-state';
import { ReservationStatusTag } from '@features/reservations/components/reservation-status-tag/reservation-status-tag';

@Component({
    selector: 'app-manager-dashboard',
    standalone: true,
    imports: [CommonModule, RouterModule, TranslatePipe, ButtonModule, CardModule, SkeletonModule, TooltipModule, PageHero, EmptyState, ReservationStatusTag],
    template: `
        <app-page-hero [title]="saudacao()" [description]="'dashboard.subtitle' | translate" [breadcrumb]="breadcrumb()">
            <p-button [label]="'dashboard.actions.reviewRequests' | translate" icon="pi pi-inbox" routerLink="/manager/approvals" />
            <p-button [label]="'dashboard.actions.newBlock' | translate" icon="pi pi-ban" severity="secondary" [outlined]="true" routerLink="/manager/blocks" />
        </app-page-hero>

        <div class="kpi-grid">
            <div class="kpi-card kpi-card--destaque">
                <div class="kpi-card__cabecalho">
                    <span class="kpi-card__rotulo">{{ 'dashboard.kpis.pending' | translate }}</span>
                    <i class="pi pi-inbox"></i>
                </div>
                <div class="kpi-card__valor">
                    @if (loading()) {
                        <p-skeleton width="3rem" height="2.4rem" />
                    } @else {
                        {{ totalPendentes() }}
                    }
                </div>
                <p-button [label]="'dashboard.actions.reviewNow' | translate" icon="pi pi-arrow-right" iconPos="right" [text]="true" size="small" routerLink="/manager/approvals" />
            </div>

            <div class="kpi-card">
                <div class="kpi-card__cabecalho">
                    <span class="kpi-card__rotulo">{{ 'dashboard.kpis.next24h' | translate }}</span>
                    <i class="pi pi-clock"></i>
                </div>
                <div class="kpi-card__valor">
                    @if (loading()) {
                        <p-skeleton width="3rem" height="2.4rem" />
                    } @else {
                        {{ proximas24h() }}
                    }
                </div>
                <span class="kpi-card__legenda">{{ 'dashboard.kpis.next24hHelp' | translate }}</span>
            </div>

            <div class="kpi-card" [class.kpi-card--alerta]="atrasados() > 0">
                <div class="kpi-card__cabecalho">
                    <span class="kpi-card__rotulo">{{ 'dashboard.kpis.late' | translate }}</span>
                    <i class="pi pi-exclamation-triangle"></i>
                </div>
                <div class="kpi-card__valor">
                    @if (loading()) {
                        <p-skeleton width="3rem" height="2.4rem" />
                    } @else {
                        {{ atrasados() }}
                    }
                </div>
                <span class="kpi-card__legenda">
                    {{ 'dashboard.kpis.lateHelp' | translate }}
                </span>
            </div>

            <div class="kpi-card">
                <div class="kpi-card__cabecalho">
                    <span class="kpi-card__rotulo">{{ 'dashboard.kpis.pavilionsPending' | translate }}</span>
                    <i class="pi pi-building"></i>
                </div>
                <div class="kpi-card__valor">
                    @if (loading()) {
                        <p-skeleton width="3rem" height="2.4rem" />
                    } @else {
                        {{ pavilhoesComPendencia() }}
                    }
                </div>
                <span class="kpi-card__legenda">
                    {{ 'dashboard.kpis.pavilionsPendingHelp' | translate: { total: totalPavilhoes() } }}
                </span>
            </div>
        </div>

        <div class="dashboard-grid">
            <section class="card dashboard-bloco">
                <header class="dashboard-bloco__header">
                    <h3 class="dashboard-bloco__titulo">
                        <i class="pi pi-inbox"></i>
                        {{ 'dashboard.sections.upcomingRequests' | translate }}
                    </h3>
                    <a routerLink="/manager/approvals" class="dashboard-bloco__link"> {{ 'dashboard.actions.viewAll' | translate }} <i class="pi pi-arrow-right"></i> </a>
                </header>

                @if (loading()) {
                    <div class="space-y-2">
                        <p-skeleton height="3rem" />
                        <p-skeleton height="3rem" />
                        <p-skeleton height="3rem" />
                    </div>
                } @else if (proximosPendentes().length === 0) {
                    <app-empty-state icone="pi pi-check-circle" [titulo]="'dashboard.sections.empty' | translate" [descricao]="'dashboard.sections.emptyDescription' | translate" [compact]="true" />
                } @else {
                    <ul class="dashboard-lista">
                        @for (r of proximosPendentes(); track r.id) {
                            <li class="dashboard-lista__item">
                                <div class="dashboard-lista__principal">
                                    <strong>{{ r.resourceName }}</strong>
                                    <span class="dashboard-lista__meta">
                                        <i class="pi pi-map-marker"></i> {{ r.pavilionName }}
                                        ·
                                        <i class="pi pi-clock"></i>
                                        {{ r.startUtc | date: 'dd/MM HH:mm' }}
                                    </span>
                                </div>
                                <app-reservation-status-tag [status]="r.status" />
                            </li>
                        }
                    </ul>
                }
            </section>

            <section class="card dashboard-bloco">
                <header class="dashboard-bloco__header">
                    <h3 class="dashboard-bloco__titulo">
                        <i class="pi pi-bolt"></i>
                        {{ 'dashboard.sections.shortcuts' | translate }}
                    </h3>
                </header>
                <div class="dashboard-atalhos">
                    <a routerLink="/manager/approvals" class="atalho">
                        <i class="pi pi-check-square"></i>
                        <div>
                            <strong>{{ 'dashboard.shortcuts.requests' | translate }}</strong>
                            <span>{{ 'dashboard.shortcuts.requestsDescription' | translate }}</span>
                        </div>
                    </a>
                    <a routerLink="/manager/blocks" class="atalho">
                        <i class="pi pi-ban"></i>
                        <div>
                            <strong>{{ 'dashboard.shortcuts.blocks' | translate }}</strong>
                            <span>{{ 'dashboard.shortcuts.blocksDescription' | translate }}</span>
                        </div>
                    </a>
                    @if (mostrarUsuarios()) {
                        <a routerLink="/manager/users" class="atalho">
                            <i class="pi pi-users"></i>
                            <div>
                                <strong>{{ 'dashboard.shortcuts.users' | translate }}</strong>
                                <span>{{ 'dashboard.shortcuts.usersDescription' | translate }}</span>
                            </div>
                        </a>
                    }
                </div>
            </section>
        </div>
    `,
    styles: [
        `
            .kpi-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
                gap: 1rem;
                margin-bottom: 1.25rem;
            }
            .kpi-card {
                background: var(--p-card-background, var(--surface-card, var(--surface-0)));
                border: 1px solid var(--surface-border);
                border-radius: 0.75rem;
                padding: 1.1rem 1.2rem;
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
                position: relative;
                overflow: hidden;
            }
            .kpi-card--destaque {
                background: linear-gradient(135deg, color-mix(in srgb, var(--primary-color), transparent 88%), color-mix(in srgb, var(--primary-color), transparent 96%));
                border-color: color-mix(in srgb, var(--primary-color), transparent 70%);
            }
            .kpi-card--alerta {
                border-color: color-mix(in srgb, var(--p-red-500, #ef4444), transparent 60%);
                background: color-mix(in srgb, var(--p-red-500, #ef4444), transparent 92%);
            }
            .kpi-card__cabecalho {
                display: flex;
                justify-content: space-between;
                align-items: center;
                color: var(--text-color-secondary);
            }
            .kpi-card__rotulo {
                font-size: 0.78rem;
                text-transform: uppercase;
                letter-spacing: 0.04em;
                font-weight: 600;
            }
            .kpi-card__cabecalho i {
                font-size: 1.1rem;
                opacity: 0.7;
            }
            .kpi-card--destaque .kpi-card__cabecalho i,
            .kpi-card--alerta .kpi-card__cabecalho i {
                opacity: 1;
                color: var(--primary-color);
            }
            .kpi-card--alerta .kpi-card__cabecalho i {
                color: var(--p-red-500, #ef4444);
            }
            .kpi-card__valor {
                font-size: 2.4rem;
                font-weight: 700;
                line-height: 1;
                color: var(--text-color);
            }
            .kpi-card__legenda {
                font-size: 0.78rem;
                color: var(--text-color-secondary);
                line-height: 1.35;
            }

            .dashboard-grid {
                display: grid;
                grid-template-columns: 2fr 1fr;
                gap: 1rem;
            }
            @media (max-width: 960px) {
                .dashboard-grid {
                    grid-template-columns: 1fr;
                }
            }

            .dashboard-bloco {
                display: flex;
                flex-direction: column;
                gap: 0.85rem;
            }
            .dashboard-bloco__header {
                display: flex;
                justify-content: space-between;
                align-items: center;
            }
            .dashboard-bloco__titulo {
                display: inline-flex;
                align-items: center;
                gap: 0.5rem;
                margin: 0;
                font-size: 1rem;
                font-weight: 600;
            }
            .dashboard-bloco__titulo i {
                color: var(--primary-color);
            }
            .dashboard-bloco__link {
                font-size: 0.85rem;
                color: var(--primary-color);
                text-decoration: none;
                display: inline-flex;
                gap: 0.25rem;
                align-items: center;
            }
            .dashboard-bloco__link:hover {
                text-decoration: underline;
            }

            .dashboard-lista {
                list-style: none;
                margin: 0;
                padding: 0;
                display: flex;
                flex-direction: column;
                gap: 0.45rem;
            }
            .dashboard-lista__item {
                display: flex;
                justify-content: space-between;
                align-items: center;
                gap: 0.75rem;
                padding: 0.65rem 0.85rem;
                border: 1px solid var(--surface-border);
                border-radius: 0.55rem;
                background: var(--surface-section, transparent);
            }
            .dashboard-lista__principal {
                display: flex;
                flex-direction: column;
                min-width: 0;
            }
            .dashboard-lista__principal strong {
                color: var(--text-color);
                font-weight: 600;
            }
            .dashboard-lista__meta {
                font-size: 0.78rem;
                color: var(--text-color-secondary);
                display: inline-flex;
                gap: 0.3rem;
                align-items: center;
                flex-wrap: wrap;
            }
            .dashboard-lista__meta i {
                color: var(--primary-color);
            }

            .dashboard-atalhos {
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
            }
            .atalho {
                display: flex;
                align-items: center;
                gap: 0.85rem;
                padding: 0.75rem 0.85rem;
                border: 1px solid var(--surface-border);
                border-radius: 0.55rem;
                color: var(--text-color);
                text-decoration: none;
                transition:
                    background-color 0.15s,
                    border-color 0.15s;
            }
            .atalho:hover {
                background: color-mix(in srgb, var(--primary-color), transparent 92%);
                border-color: color-mix(in srgb, var(--primary-color), transparent 70%);
            }
            .atalho i {
                font-size: 1.4rem;
                color: var(--primary-color);
                width: 1.6rem;
                text-align: center;
            }
            .atalho strong {
                display: block;
                font-weight: 600;
                line-height: 1.2;
            }
            .atalho span {
                font-size: 0.78rem;
                color: var(--text-color-secondary);
            }
        `
    ]
})
export class DashboardPage {
    private readonly api = inject(ApprovalsService);
    private readonly pavilionsApi = inject(PavilionService);
    private readonly auth = inject(AuthService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly loading = signal(true);
    readonly pending = signal<Reservation[]>([]);
    readonly totalPavilhoes = signal(0);

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('menu.operations') }, { label: this.translate.instant('dashboard.title') }];
    });

    readonly totalPendentes = computed(() => this.pending().length);
    readonly atrasados = computed(() => {
        const agora = Date.now();
        return this.pending().filter((r) => new Date(r.startUtc).getTime() < agora).length;
    });
    readonly proximas24h = computed(() => {
        const agora = Date.now();
        const limite = agora + 24 * 60 * 60 * 1000;
        return this.pending().filter((r) => {
            const t = new Date(r.startUtc).getTime();
            return t >= agora && t <= limite;
        }).length;
    });
    readonly pavilhoesComPendencia = computed(() => new Set(this.pending().map((r) => r.pavilionId)).size);
    readonly proximosPendentes = computed(() => [...this.pending()].sort((a, b) => +new Date(a.startUtc) - +new Date(b.startUtc)).slice(0, 5));

    readonly mostrarUsuarios = computed(() => this.auth.isAdmin());

    readonly saudacao = computed(() => {
        void this.language.atual();
        const h = new Date().getHours();
        const nome = this.auth.currentUser()?.fullName?.split(' ')[0] ?? '';
        const chave = h < 12 ? 'dashboard.greeting.morning' : h < 18 ? 'dashboard.greeting.afternoon' : 'dashboard.greeting.evening';
        const periodo = this.translate.instant(chave);
        return nome ? `${periodo}, ${nome}` : `${periodo}`;
    });

    constructor() {
        this.api.listPending(null).subscribe({
            next: (list) => {
                this.pending.set(list);
                this.loading.set(false);
            },
            error: () => this.loading.set(false)
        });
        this.pavilionsApi.list().subscribe({
            next: (list) => this.totalPavilhoes.set(list.length)
        });
    }
}
