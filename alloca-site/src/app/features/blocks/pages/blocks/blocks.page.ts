import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { BlockListItem, BlockTargetType } from '@/app/features/blocks/models/block.model';
import { Pavilion } from '@/app/features/pavilions/models/pavilion.model';
import { BlocksService } from '@/app/features/blocks/services/blocks.service';
import { PavilionService } from '@/app/features/pavilions/services/pavilion.service';
import { LanguageService } from '@/app/core/i18n/language.service';
import { PageHero } from '@/app/shared/components/page-hero/page-hero';
import { Loader } from '@/app/shared/components/loader/loader';
import { EmptyState } from '@/app/shared/components/empty-state/empty-state';

const TIPO_ICON: Record<BlockTargetType, string> = {
    Pavilion: 'pi pi-building',
    Room: 'pi pi-th-large',
    Desk: 'pi pi-stop'
};

@Component({
    selector: 'app-manager-blocks',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, TranslatePipe, ButtonModule, IconFieldModule, InputIconModule, InputTextModule, SelectModule, TableModule, TagModule, ToggleSwitchModule, TooltipModule, PageHero, Loader, EmptyState],
    template: `
        <app-page-hero [title]="'blocks.list.title' | translate" [description]="'blocks.list.subtitle' | translate" icone="pi-shield" [breadcrumb]="breadcrumb()">
            <p-button [label]="'common.actions.refresh' | translate" icon="pi pi-refresh" severity="secondary" [outlined]="true" [loading]="carregando()" (onClick)="carregar()" />
            <p-button [label]="'blocks.list.newButton' | translate" icon="pi pi-plus" routerLink="/manager/blocks/novo" />
        </app-page-hero>

        <div class="card">
            <div class="bloqueios-toolbar">
                <p-iconfield iconPosition="left" styleClass="bloqueios-busca">
                    <p-inputicon><i class="pi pi-search"></i></p-inputicon>
                    <input pInputText type="text" [placeholder]="'blocks.list.searchPlaceholder' | translate" [(ngModel)]="textoBusca" class="w-full" />
                </p-iconfield>
                <p-select [options]="pavilionOptions()" optionLabel="label" optionValue="value" [(ngModel)]="filtroPavilhao" appendTo="body" styleClass="w-72" [placeholder]="'blocks.list.allPavilions' | translate" (onChange)="carregar()" />
                <label class="bloqueios-toggle">
                    <p-toggleSwitch [(ngModel)]="incluirExpirados" (onChange)="carregar()" />
                    <span>{{ 'blocks.list.includeExpired' | translate }}</span>
                </label>
            </div>

            @if (carregando()) {
                <app-loader [rotulo]="'blocks.list.loading' | translate" />
            } @else if (filtrados().length === 0) {
                <app-empty-state icone="pi pi-shield" [titulo]="'blocks.list.empty' | translate" [descricao]="(bloqueios().length === 0 ? 'blocks.list.emptyCreate' : 'blocks.list.emptyFiltered') | translate" />
            } @else {
                <p-table [value]="filtrados()" dataKey="id" [paginator]="true" [rows]="10" [rowsPerPageOptions]="[10, 25, 50]" responsiveLayout="scroll" sortField="startUtc" [sortOrder]="-1">
                    <ng-template pTemplate="header">
                        <tr>
                            <th>{{ 'blocks.list.columns.type' | translate }}</th>
                            <th pSortableColumn="targetName">{{ 'blocks.list.columns.target' | translate }} <p-sortIcon field="targetName" /></th>
                            <th pSortableColumn="pavilionName">{{ 'blocks.list.columns.pavilion' | translate }} <p-sortIcon field="pavilionName" /></th>
                            <th pSortableColumn="startUtc">{{ 'blocks.list.columns.period' | translate }} <p-sortIcon field="startUtc" /></th>
                            <th>{{ 'blocks.list.columns.reason' | translate }}</th>
                            <th>{{ 'blocks.list.columns.status' | translate }}</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-b>
                        <tr>
                            <td>
                                <span class="bloqueio-tipo">
                                    <i [class]="iconeTipo(b.targetType)"></i>
                                    {{ rotuloTipo(b.targetType) }}
                                </span>
                            </td>
                            <td>
                                <div class="font-medium">{{ b.targetName }}</div>
                            </td>
                            <td>{{ b.pavilionName || '—' }}</td>
                            <td>
                                <div>{{ b.startUtc | date: 'dd/MM/yyyy HH:mm' }}</div>
                                <div class="text-xs text-muted-color">{{ 'blocks.list.until' | translate }} {{ b.endUtc | date: 'dd/MM/yyyy HH:mm' }}</div>
                            </td>
                            <td class="bloqueio-motivo">
                                <span [pTooltip]="b.reason" tooltipPosition="top">{{ b.reason }}</span>
                            </td>
                            <td>
                                @if (b.isActive) {
                                    <p-tag [value]="'blocks.list.statusActive' | translate" severity="danger" icon="pi pi-ban" />
                                } @else {
                                    <p-tag [value]="'blocks.list.statusExpired' | translate" severity="secondary" icon="pi pi-clock" />
                                }
                            </td>
                        </tr>
                    </ng-template>
                </p-table>
            }
        </div>
    `,
    styles: [
        `
            .bloqueios-toolbar {
                display: flex;
                gap: 0.75rem;
                align-items: center;
                flex-wrap: wrap;
                margin-bottom: 1rem;
            }
            .bloqueios-busca {
                flex: 1 1 18rem;
                min-width: 16rem;
            }
            .bloqueios-toggle {
                display: inline-flex;
                gap: 0.5rem;
                align-items: center;
                font-size: 0.85rem;
                color: var(--text-color-secondary);
                cursor: pointer;
            }
            .bloqueio-tipo {
                display: inline-flex;
                align-items: center;
                gap: 0.4rem;
                font-size: 0.85rem;
            }
            .bloqueio-tipo i {
                color: var(--primary-color);
            }
            .bloqueio-motivo {
                max-width: 18rem;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
        `
    ]
})
export class BlocksPage {
    private readonly api = inject(BlocksService);
    private readonly pavApi = inject(PavilionService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly carregando = signal(false);
    readonly bloqueios = signal<BlockListItem[]>([]);
    readonly pavilhoes = signal<Pavilion[]>([]);

    textoBusca = '';
    filtroPavilhao: string | null = null;
    incluirExpirados = false;

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('menu.operations') }, { label: this.translate.instant('blocks.list.title') }];
    });

    readonly pavilionOptions = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('blocks.list.allPavilions'), value: null as string | null }, ...this.pavilhoes().map((p) => ({ label: p.name, value: p.id as string | null }))];
    });

    readonly filtrados = computed(() => {
        const termo = this.textoBusca.trim().toLowerCase();
        if (!termo) return this.bloqueios();
        return this.bloqueios().filter((b) => b.targetName.toLowerCase().includes(termo) || (b.pavilionName ?? '').toLowerCase().includes(termo) || b.reason.toLowerCase().includes(termo));
    });

    constructor() {
        this.pavApi.list().subscribe((list) => this.pavilhoes.set(list));
        this.carregar();
    }

    carregar(): void {
        this.carregando.set(true);
        this.api
            .listBlocks({
                pavilionId: this.filtroPavilhao,
                includeExpired: this.incluirExpirados
            })
            .subscribe({
                next: (lista) => {
                    this.bloqueios.set(lista);
                    this.carregando.set(false);
                },
                error: () => {
                    this.bloqueios.set([]);
                    this.carregando.set(false);
                }
            });
    }

    rotuloTipo(t: BlockTargetType): string {
        return this.translate.instant(`blocks.list.targets.${t}`);
    }

    iconeTipo(t: BlockTargetType): string {
        return TIPO_ICON[t] ?? 'pi pi-ban';
    }
}
