import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { BlockTargetType } from '@features/blocks/models/block.model';
import { Floor, FloorRoomResource, Pavilion } from '@features/pavilions/models/pavilion.model';
import { BlocksService } from '@features/blocks/services/blocks.service';
import { PavilionService } from '@features/pavilions/services/pavilion.service';
import { LanguageService } from '@core/i18n/language.service';
import { PageHero } from '@shared/components/page-hero/page-hero';

interface OpcaoTipo {
    value: BlockTargetType;
    icon: string;
    labelKey: string;
    descKey: string;
}

const OPCOES_TIPO: OpcaoTipo[] = [
    { value: 'Pavilion', icon: 'pi pi-building', labelKey: 'blocks.create.scopes.pavilion', descKey: 'blocks.create.scopes.pavilionDescription' },
    { value: 'Room', icon: 'pi pi-th-large', labelKey: 'blocks.create.scopes.room', descKey: 'blocks.create.scopes.roomDescription' },
    { value: 'Desk', icon: 'pi pi-stop', labelKey: 'blocks.create.scopes.desk', descKey: 'blocks.create.scopes.deskDescription' }
];

interface OpcaoRecurso {
    label: string;
    value: string;
    descricao?: string;
}

@Component({
    selector: 'app-manager-block-create',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, ButtonModule, DatePickerModule, DividerModule, InputTextModule, MessageModule, SelectModule, TextareaModule, PageHero],
    template: `
        <app-page-hero [title]="'blocks.create.title' | translate" [description]="'blocks.create.subtitle' | translate" icone="pi-ban" [breadcrumb]="breadcrumb()">
            <p-button [label]="'blocks.create.back' | translate" icon="pi pi-arrow-left" severity="secondary" [outlined]="true" routerLink="/manager/blocks" />
        </app-page-hero>

        <form (ngSubmit)="enviar()" #form="ngForm" novalidate class="bloqueio-form">
            <div class="card bloqueio-card">
                <header class="bloqueio-secao__cabecalho">
                    <span class="bloqueio-secao__num">1</span>
                    <div>
                        <h2 class="bloqueio-secao__titulo">{{ 'blocks.create.section1.title' | translate }}</h2>
                        <p class="bloqueio-secao__desc">{{ 'blocks.create.section1.description' | translate }}</p>
                    </div>
                </header>

                <div class="tipo-grid" role="radiogroup" [attr.aria-label]="'blocks.create.section1.groupLabel' | translate">
                    @for (opt of opcoesTipo; track opt.value) {
                        <button type="button" class="tipo-card" [class.tipo-card--ativo]="targetType() === opt.value" [attr.aria-checked]="targetType() === opt.value" role="radio" (click)="selecionarTipo(opt.value)">
                            <span class="tipo-card__topo">
                                <i class="tipo-card__icone" [ngClass]="opt.icon" aria-hidden="true"></i>
                                @if (targetType() === opt.value) {
                                    <i class="pi pi-check-circle tipo-card__check" aria-hidden="true"></i>
                                }
                            </span>
                            <strong>{{ opt.labelKey | translate }}</strong>
                            <span class="tipo-card__desc">{{ opt.descKey | translate }}</span>
                        </button>
                    }
                </div>
            </div>

            <div class="card bloqueio-card">
                <header class="bloqueio-secao__cabecalho">
                    <span class="bloqueio-secao__num">2</span>
                    <div>
                        <h2 class="bloqueio-secao__titulo">{{ 'blocks.create.section2.title' | translate }}</h2>
                        <p class="bloqueio-secao__desc">
                            @switch (targetType()) {
                                @case ('Pavilion') {
                                    {{ 'blocks.create.section2.descriptionPavilion' | translate }}
                                }
                                @case ('Room') {
                                    {{ 'blocks.create.section2.descriptionRoom' | translate }}
                                }
                                @case ('Desk') {
                                    {{ 'blocks.create.section2.descriptionDesk' | translate }}
                                }
                            }
                        </p>
                    </div>
                </header>

                <div class="form-grid">
                    <div class="campo">
                        <label class="campo-rotulo" for="pavilhao"> {{ 'blocks.create.pavilionLabel' | translate }} <span class="obrig">*</span> </label>
                        <p-select
                            inputId="pavilhao"
                            [options]="pavilionOptions()"
                            optionLabel="label"
                            optionValue="value"
                            [ngModel]="pavilhaoId()"
                            (ngModelChange)="aoMudarPavilhao($event)"
                            name="pavilhaoId"
                            [placeholder]="'blocks.create.selectPavilion' | translate"
                            appendTo="body"
                            styleClass="w-full"
                            [loading]="carregandoPavilhoes()"
                            [showClear]="true"
                        />
                    </div>

                    @if (targetType() !== 'Pavilion') {
                        <div class="campo">
                            <label class="campo-rotulo" for="andar"> {{ 'blocks.create.floorLabel' | translate }} <span class="obrig">*</span> </label>
                            <p-select
                                inputId="andar"
                                [options]="floorOptions()"
                                optionLabel="label"
                                optionValue="value"
                                [ngModel]="andarId()"
                                (ngModelChange)="aoMudarAndar($event)"
                                name="andarId"
                                [placeholder]="placeholderAndar()"
                                appendTo="body"
                                styleClass="w-full"
                                [disabled]="!pavilhaoId() || carregandoAndares()"
                                [loading]="carregandoAndares()"
                                [showClear]="true"
                            />
                        </div>

                        <div class="campo col-span-full">
                            <label class="campo-rotulo" for="recurso"> {{ (targetType() === 'Room' ? 'blocks.create.roomLabel' : 'blocks.create.deskLabel') | translate }} <span class="obrig">*</span> </label>
                            <p-select
                                inputId="recurso"
                                [options]="recursoOptions()"
                                optionLabel="label"
                                optionValue="value"
                                [ngModel]="recursoId()"
                                (ngModelChange)="recursoId.set($event)"
                                name="recursoId"
                                [placeholder]="placeholderRecurso()"
                                appendTo="body"
                                styleClass="w-full"
                                [filter]="true"
                                filterBy="label,descricao"
                                [disabled]="!andarId() || carregandoRecursos()"
                                [loading]="carregandoRecursos()"
                                [showClear]="true"
                            >
                                <ng-template let-opt pTemplate="item">
                                    <div class="recurso-item">
                                        <span class="recurso-item__titulo">{{ opt.label }}</span>
                                        @if (opt.descricao) {
                                            <span class="recurso-item__desc">{{ opt.descricao }}</span>
                                        }
                                    </div>
                                </ng-template>
                            </p-select>

                            @if (andarId() && !carregandoRecursos() && recursoOptions().length === 0) {
                                <p-message severity="warn" styleClass="mt-2">
                                    {{ (targetType() === 'Room' ? 'blocks.create.emptyRooms' : 'blocks.create.emptyDesks') | translate }}
                                </p-message>
                            }
                        </div>
                    }
                </div>
            </div>

            <div class="card bloqueio-card">
                <header class="bloqueio-secao__cabecalho">
                    <span class="bloqueio-secao__num">3</span>
                    <div>
                        <h2 class="bloqueio-secao__titulo">{{ 'blocks.create.section3.title' | translate }}</h2>
                        <p class="bloqueio-secao__desc">{{ 'blocks.create.section3.description' | translate }}</p>
                    </div>
                </header>

                <div class="form-grid">
                    <div class="campo">
                        <label class="campo-rotulo" for="inicio"> {{ 'blocks.create.startLabel' | translate }} <span class="obrig">*</span> </label>
                        <p-datepicker
                            inputId="inicio"
                            [ngModel]="inicio()"
                            (ngModelChange)="aoMudarInicio($event)"
                            name="inicio"
                            [showTime]="true"
                            hourFormat="24"
                            [stepMinute]="15"
                            [minDate]="agora()"
                            appendTo="body"
                            dateFormat="dd/mm/yy"
                            [placeholder]="'blocks.create.datePlaceholder' | translate"
                            styleClass="w-full"
                        />
                    </div>
                    <div class="campo">
                        <label class="campo-rotulo" for="termino"> {{ 'blocks.create.endLabel' | translate }} <span class="obrig">*</span> </label>
                        <p-datepicker
                            inputId="termino"
                            [ngModel]="termino()"
                            (ngModelChange)="termino.set($event)"
                            name="termino"
                            [showTime]="true"
                            hourFormat="24"
                            [stepMinute]="15"
                            [minDate]="inicio() ?? agora()"
                            appendTo="body"
                            dateFormat="dd/mm/yy"
                            [placeholder]="'blocks.create.datePlaceholder' | translate"
                            styleClass="w-full"
                        />
                    </div>

                    @if (duracaoLabel()) {
                        <div class="col-span-full duracao">
                            <i class="pi pi-clock" aria-hidden="true"></i>
                            <span
                                >{{ 'blocks.create.section3.durationLabel' | translate }} <strong>{{ duracaoLabel() }}</strong></span
                            >
                        </div>
                    }

                    @if (erroPeriodo()) {
                        <div class="col-span-full">
                            <p-message severity="error">{{ erroPeriodo() }}</p-message>
                        </div>
                    }
                </div>
            </div>

            <div class="card bloqueio-card">
                <header class="bloqueio-secao__cabecalho">
                    <span class="bloqueio-secao__num">4</span>
                    <div>
                        <h2 class="bloqueio-secao__titulo">{{ 'blocks.create.section4.title' | translate }}</h2>
                        <p class="bloqueio-secao__desc">{{ 'blocks.create.section4.description' | translate }}</p>
                    </div>
                </header>

                <div class="campo">
                    <textarea pTextarea [(ngModel)]="motivo" name="motivo" rows="3" class="w-full" maxlength="280" [placeholder]="'blocks.create.section4.placeholder' | translate" required></textarea>
                    <div class="campo-ajuda">
                        <span [class.text-danger]="motivo.trim().length > 0 && motivo.trim().length < 5">
                            {{ 'blocks.create.section4.minHint' | translate }}
                        </span>
                        <span class="campo-ajuda__contador">{{ motivo.length }}/280</span>
                    </div>
                </div>
            </div>

            <div class="bloqueio-resumo">
                <div class="bloqueio-resumo__texto">
                    <strong>{{ 'blocks.create.summary.label' | translate }}</strong>
                    @if (resumo(); as r) {
                        {{ r }}
                    } @else {
                        <span class="text-muted-color">{{ 'blocks.create.summary.placeholder' | translate }}</span>
                    }
                </div>
                <div class="bloqueio-resumo__acoes">
                    <p-button type="button" [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" routerLink="/manager/blocks" />
                    <p-button type="submit" [label]="'blocks.create.submit' | translate" icon="pi pi-ban" [loading]="enviando()" [disabled]="!ehValido() || enviando()" />
                </div>
            </div>
        </form>
    `,
    styles: [
        `
            .bloqueio-form {
                display: flex;
                flex-direction: column;
                gap: 1rem;
            }
            .bloqueio-card {
                margin: 0;
            }
            .bloqueio-secao__cabecalho {
                display: flex;
                align-items: flex-start;
                gap: 0.85rem;
                margin-bottom: 1rem;
            }
            .bloqueio-secao__num {
                width: 1.85rem;
                height: 1.85rem;
                border-radius: 999px;
                background: color-mix(in srgb, var(--primary-color), transparent 85%);
                color: var(--primary-color);
                display: inline-flex;
                align-items: center;
                justify-content: center;
                font-weight: 700;
                font-size: 0.9rem;
                flex: none;
            }
            .bloqueio-secao__titulo {
                margin: 0;
                font-size: 1rem;
                font-weight: 600;
                color: var(--text-color);
            }
            .bloqueio-secao__desc {
                margin: 0.15rem 0 0;
                font-size: 0.85rem;
                color: var(--text-color-secondary);
            }
            .form-grid {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 1rem;
            }
            @media (max-width: 720px) {
                .form-grid {
                    grid-template-columns: 1fr;
                }
            }
            .col-span-full {
                grid-column: 1 / -1;
            }
            .campo {
                display: flex;
                flex-direction: column;
            }
            .campo-rotulo {
                display: block;
                font-weight: 500;
                margin-bottom: 0.4rem;
                font-size: 0.88rem;
                color: var(--text-color);
            }
            .obrig {
                color: var(--red-500, #ef4444);
            }
            .campo-ajuda {
                display: flex;
                justify-content: space-between;
                font-size: 0.78rem;
                color: var(--text-color-secondary);
                margin-top: 0.35rem;
            }
            .campo-ajuda .text-danger {
                color: var(--red-500, #ef4444);
            }
            .tipo-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
                gap: 0.75rem;
            }
            .tipo-card {
                position: relative;
                background: var(--surface-card);
                border: 1px solid var(--surface-border);
                border-radius: 0.7rem;
                padding: 0.95rem 1rem;
                text-align: left;
                cursor: pointer;
                display: flex;
                flex-direction: column;
                gap: 0.3rem;
                transition: all 0.15s;
                color: var(--text-color);
            }
            .tipo-card:hover {
                border-color: color-mix(in srgb, var(--primary-color), transparent 40%);
                transform: translateY(-1px);
            }
            .tipo-card--ativo {
                background: color-mix(in srgb, var(--primary-color), transparent 90%);
                border-color: var(--primary-color);
                box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color), transparent 80%);
            }
            .tipo-card__topo {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }
            .tipo-card__icone {
                font-size: 1.25rem;
                color: var(--primary-color);
            }
            .tipo-card__check {
                color: var(--primary-color);
                font-size: 1.1rem;
            }
            .tipo-card strong {
                font-size: 0.95rem;
            }
            .tipo-card__desc {
                color: var(--text-color-secondary);
                font-size: 0.8rem;
                line-height: 1.35;
            }
            .recurso-item {
                display: flex;
                flex-direction: column;
                gap: 0.1rem;
            }
            .recurso-item__titulo {
                font-weight: 500;
            }
            .recurso-item__desc {
                font-size: 0.78rem;
                color: var(--text-color-secondary);
            }
            .duracao {
                display: inline-flex;
                align-items: center;
                gap: 0.5rem;
                font-size: 0.85rem;
                color: var(--text-color-secondary);
                background: color-mix(in srgb, var(--primary-color), transparent 92%);
                padding: 0.55rem 0.85rem;
                border-radius: 0.5rem;
                width: fit-content;
            }
            .duracao i {
                color: var(--primary-color);
            }
            .bloqueio-resumo {
                background: var(--surface-card);
                border: 1px solid var(--surface-border);
                border-radius: 0.875rem;
                padding: 1rem 1.25rem;
                display: flex;
                align-items: center;
                gap: 1rem;
                flex-wrap: wrap;
                justify-content: space-between;
                position: sticky;
                bottom: 0;
            }
            .bloqueio-resumo__texto {
                font-size: 0.9rem;
                color: var(--text-color);
                flex: 1 1 18rem;
            }
            .bloqueio-resumo__acoes {
                display: inline-flex;
                gap: 0.5rem;
            }
        `
    ]
})
export class BlockCreatePage implements OnDestroy {
    private readonly api = inject(BlocksService);
    private readonly pavApi = inject(PavilionService);
    private readonly toast = inject(MessageService);
    private readonly router = inject(Router);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);
    private tickAgora: ReturnType<typeof setInterval> | null = null;

    readonly opcoesTipo = OPCOES_TIPO;

    readonly enviando = signal(false);
    readonly carregandoPavilhoes = signal(true);
    readonly carregandoAndares = signal(false);
    readonly carregandoRecursos = signal(false);

    readonly pavilhoes = signal<Pavilion[]>([]);
    readonly andares = signal<Floor[]>([]);
    readonly salas = signal<FloorRoomResource[]>([]);

    readonly targetType = signal<BlockTargetType>('Room');
    readonly pavilhaoId = signal<string | null>(null);
    readonly andarId = signal<string | null>(null);
    readonly recursoId = signal<string | null>(null);
    readonly inicio = signal<Date | null>(this.proximaHoraCheia());
    readonly termino = signal<Date | null>(this.somarHoras(this.proximaHoraCheia(), 1));
    readonly agora = signal<Date>(new Date());
    motivo = '';

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('menu.operations') }, { label: this.translate.instant('blocks.list.title'), rota: '/manager/blocks' }, { label: this.translate.instant('blocks.create.title') }];
    });

    readonly pavilionOptions = computed(() => this.pavilhoes().map((p) => ({ label: p.name, value: p.id })));
    readonly floorOptions = computed(() => {
        void this.language.atual();
        const nivel = this.translate.instant('blocks.create.floorLevel');
        return this.andares().map((f) => ({ label: `${f.name} · ${nivel} ${f.level}`, value: f.id }));
    });

    readonly recursoOptions = computed<OpcaoRecurso[]>(() => {
        void this.language.atual();
        const tipo = this.targetType();
        const salas = this.salas();
        if (tipo === 'Room') {
            return salas
                .filter((r) => r.isReservable)
                .map((r) => ({
                    label: r.name,
                    descricao: `${r.externalId} · ${this.translate.instant('blocks.create.deskCount', { count: r.desks.length })}`,
                    value: r.id
                }));
        }
        if (tipo === 'Desk') {
            return salas.flatMap((r) =>
                r.desks
                    .filter((d) => d.isReservable)
                    .map((d) => ({
                        label: d.name,
                        descricao: `${d.externalId} · ${r.name}`,
                        value: d.id
                    }))
            );
        }
        return [];
    });

    readonly placeholderAndar = computed(() => {
        void this.language.atual();
        if (!this.pavilhaoId()) return this.translate.instant('blocks.create.selectPavilionFirst');
        if (this.carregandoAndares()) return this.translate.instant('blocks.create.loading');
        return this.translate.instant('blocks.create.selectFloor');
    });

    readonly placeholderRecurso = computed(() => {
        void this.language.atual();
        if (!this.andarId()) return this.translate.instant('blocks.create.selectFloorFirst');
        if (this.carregandoRecursos()) return this.translate.instant('blocks.create.loading');
        return this.targetType() === 'Room' ? this.translate.instant('blocks.create.selectRoom') : this.translate.instant('blocks.create.selectDesk');
    });

    readonly erroPeriodo = computed(() => {
        void this.language.atual();
        const i = this.inicio();
        const f = this.termino();
        if (!i || !f) return this.translate.instant('blocks.create.errors.missingPeriod');
        if (i.getTime() < this.agora().getTime()) return this.translate.instant('blocks.create.errors.pastStart');
        if (i.getTime() >= f.getTime()) return this.translate.instant('blocks.create.errors.endBeforeStart');
        return null;
    });

    readonly duracaoLabel = computed(() => {
        const i = this.inicio();
        const f = this.termino();
        if (!i || !f || i.getTime() >= f.getTime()) return null;
        const ms = f.getTime() - i.getTime();
        const totalMin = Math.round(ms / 60000);
        const dias = Math.floor(totalMin / 1440);
        const horas = Math.floor((totalMin % 1440) / 60);
        const min = totalMin % 60;
        const partes: string[] = [];
        if (dias) partes.push(`${dias}d`);
        if (horas) partes.push(`${horas}h`);
        if (min) partes.push(`${min}min`);
        return partes.length ? partes.join(' ') : '0min';
    });

    readonly resumo = computed(() => {
        void this.language.atual();
        const tipo = this.targetType();
        const opt = OPCOES_TIPO.find((o) => o.value === tipo);
        const pav = this.pavilhoes().find((p) => p.id === this.pavilhaoId());
        const i = this.inicio();
        const f = this.termino();
        if (!opt) return null;
        const tipoLabel = this.translate.instant(opt.labelKey);
        const pavLabel = pav ? pav.name : '—';
        let alvoLabel = pavLabel;
        if (tipo !== 'Pavilion') {
            const recurso = this.recursoOptions().find((o) => o.value === this.recursoId());
            alvoLabel = recurso ? `${recurso.label} (${pavLabel})` : pavLabel;
        }
        if (!i || !f) return `${tipoLabel} · ${alvoLabel}`;
        return `${tipoLabel} · ${alvoLabel} · ${this.formatarData(i)} → ${this.formatarData(f)}`;
    });

    constructor() {
        this.pavApi.list().subscribe({
            next: (list) => {
                this.pavilhoes.set(list);
                this.carregandoPavilhoes.set(false);
            },
            error: () => this.carregandoPavilhoes.set(false)
        });
        this.tickAgora = setInterval(() => this.agora.set(new Date()), 60_000);
    }

    ngOnDestroy(): void {
        if (this.tickAgora) {
            clearInterval(this.tickAgora);
            this.tickAgora = null;
        }
    }

    selecionarTipo(t: BlockTargetType): void {
        if (this.targetType() === t) return;
        this.targetType.set(t);
        this.recursoId.set(null);
        if (t === 'Pavilion') {
            this.andarId.set(null);
        } else if (this.pavilhaoId() && this.andares().length === 0 && !this.carregandoAndares()) {
            this.carregarAndares(this.pavilhaoId()!);
        }
    }

    aoMudarPavilhao(id: string | null): void {
        this.pavilhaoId.set(id);
        this.andarId.set(null);
        this.recursoId.set(null);
        this.andares.set([]);
        this.salas.set([]);
        if (!id || this.targetType() === 'Pavilion') return;
        this.carregarAndares(id);
    }

    private carregarAndares(pavilhaoId: string): void {
        this.carregandoAndares.set(true);
        this.pavApi.listFloors(pavilhaoId).subscribe({
            next: (list) => {
                this.andares.set(list);
                this.carregandoAndares.set(false);
            },
            error: () => this.carregandoAndares.set(false)
        });
    }

    aoMudarAndar(id: string | null): void {
        this.andarId.set(id);
        this.recursoId.set(null);
        this.salas.set([]);
        const pav = this.pavilhaoId();
        if (!pav || !id) return;
        this.carregandoRecursos.set(true);
        this.pavApi.listResources(pav, id).subscribe({
            next: (res) => {
                this.salas.set(res.rooms);
                this.carregandoRecursos.set(false);
            },
            error: () => {
                this.salas.set([]);
                this.carregandoRecursos.set(false);
                this.toast.add({
                    severity: 'error',
                    summary: this.translate.instant('common.labels.error'),
                    detail: this.translate.instant('blocks.create.errors.errorLoadResources')
                });
            }
        });
    }

    aoMudarInicio(d: Date | null): void {
        this.inicio.set(d);
        const f = this.termino();
        if (d && (!f || f.getTime() <= d.getTime())) {
            this.termino.set(this.somarHoras(d, 1));
        }
    }

    ehValido(): boolean {
        if (this.motivo.trim().length < 5) return false;
        if (this.erroPeriodo()) return false;
        const tipo = this.targetType();
        if (tipo === 'Pavilion') return !!this.pavilhaoId();
        return !!this.pavilhaoId() && !!this.andarId() && !!this.recursoId();
    }

    enviar(): void {
        if (!this.ehValido()) return;
        const tipo = this.targetType();
        const targetId = tipo === 'Pavilion' ? this.pavilhaoId()! : this.recursoId()!;
        this.enviando.set(true);
        this.api
            .createBlock({
                targetType: tipo,
                targetId,
                startUtc: this.inicio()!.toISOString(),
                endUtc: this.termino()!.toISOString(),
                reason: this.motivo.trim()
            })
            .subscribe({
                next: () => {
                    this.enviando.set(false);
                    this.toast.add({
                        severity: 'success',
                        summary: this.translate.instant('blocks.toasts.created.summary'),
                        detail: this.translate.instant('blocks.toasts.created.detail')
                    });
                    this.router.navigate(['/manager/blocks']);
                },
                error: (err) => {
                    this.enviando.set(false);
                    this.toast.add({
                        severity: 'error',
                        summary: this.translate.instant('common.labels.error'),
                        detail: err?.error?.detail || err?.error?.title || this.translate.instant('blocks.toasts.errorCreate')
                    });
                }
            });
    }

    private proximaHoraCheia(): Date {
        const d = new Date();
        d.setMinutes(0, 0, 0);
        d.setHours(d.getHours() + 1);
        return d;
    }

    private somarHoras(base: Date | null, horas: number): Date {
        const d = base ? new Date(base) : new Date();
        d.setHours(d.getHours() + horas);
        return d;
    }

    private formatarData(d: Date): string {
        const idioma = this.language.atual();
        return d.toLocaleString(idioma, {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }
}
