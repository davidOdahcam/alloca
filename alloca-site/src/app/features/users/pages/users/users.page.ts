import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { UserListItem } from '../../models/user.model';
import { UserRole } from '@/app/core/auth/models/auth.model';
import { AuthService } from '@/app/core/auth/auth.service';
import { UserService } from '@/app/features/users/services/user.service';
import { LanguageService } from '@/app/core/i18n/language.service';
import { PageHero } from '@/app/shared/components/page-hero/page-hero';
import { EmptyState } from '@/app/shared/components/empty-state/empty-state';

interface NovoUsuarioForm {
    fullName: string;
    email: string;
    role: UserRole;
    password: string;
}

interface EditarUsuarioForm {
    id: string;
    fullName: string;
    email: string;
    role: UserRole;
}

interface RedefinirSenhaForm {
    id: string;
    fullName: string;
    newPassword: string;
}

@Component({
    selector: 'app-manager-users',
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
        MessageModule,
        ProgressSpinnerModule,
        SelectModule,
        TableModule,
        TagModule,
        ToastModule,
        TooltipModule,
        PageHero,
        EmptyState
    ],
    providers: [MessageService, ConfirmationService],
    template: `
        <p-toast />
        <p-confirmDialog />

        <app-page-hero [title]="'users.list.title' | translate" [description]="'users.list.subtitle' | translate" [breadcrumb]="breadcrumb()">
            <p-button [label]="'users.list.newButton' | translate" icon="pi pi-plus" (onClick)="abrirCadastro()" />
        </app-page-hero>

        <div class="card">
            <div class="usuarios-toolbar">
                <p-iconfield iconPosition="left" styleClass="usuarios-busca">
                    <p-inputicon><i class="pi pi-search"></i></p-inputicon>
                    <input pInputText type="text" [placeholder]="'users.list.searchPlaceholder' | translate" [(ngModel)]="busca" (ngModelChange)="agendarBusca()" class="w-full" />
                </p-iconfield>
                <p-select [options]="filtrosPapel()" optionLabel="label" optionValue="value" [(ngModel)]="papelFiltro" (ngModelChange)="recarregar()" appendTo="body" styleClass="w-60" />
                <p-select [options]="filtrosStatus()" optionLabel="label" optionValue="value" [(ngModel)]="statusFiltro" (ngModelChange)="recarregar()" appendTo="body" styleClass="w-44" />
                <p-button icon="pi pi-refresh" severity="secondary" [outlined]="true" [pTooltip]="'common.actions.refresh' | translate" (onClick)="recarregar()" />
            </div>

            @if (carregando()) {
                <div class="usuarios-carregando">
                    <p-progressSpinner strokeWidth="4" />
                </div>
            } @else if (usuarios().length === 0) {
                <app-empty-state icone="pi pi-users" [titulo]="'users.list.empty' | translate" [descricao]="'users.list.emptyDescription' | translate" [compact]="true" />
            } @else {
                <p-table [value]="usuarios()" [paginator]="true" [rows]="10" dataKey="id">
                    <ng-template pTemplate="header">
                        <tr>
                            <th>{{ 'users.list.columns.name' | translate }}</th>
                            <th>{{ 'users.list.columns.email' | translate }}</th>
                            <th>{{ 'users.list.columns.role' | translate }}</th>
                            <th>{{ 'users.list.columns.status' | translate }}</th>
                            <th class="text-right">{{ 'users.list.columns.actions' | translate }}</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-u>
                        <tr>
                            <td class="font-medium">
                                {{ u.fullName }}
                                @if (u.id === idLogado()) {
                                    <p-tag [value]="'common.labels.you' | translate" severity="info" styleClass="ml-2" />
                                }
                            </td>
                            <td>{{ u.email }}</td>
                            <td>
                                <p-tag [value]="rotuloPapel(u.role)" [severity]="severidadePapel(u.role)" />
                            </td>
                            <td>
                                <p-tag [value]="(u.isActive ? 'common.status.active' : 'common.status.inactive') | translate" [severity]="u.isActive ? 'success' : 'secondary'" />
                            </td>
                            <td class="text-right whitespace-nowrap">
                                <p-button icon="pi pi-pencil" [text]="true" [rounded]="true" [pTooltip]="'users.list.tooltips.edit' | translate" (onClick)="abrirEdicao(u)" />
                                <p-button icon="pi pi-key" [text]="true" [rounded]="true" severity="secondary" [pTooltip]="'users.list.tooltips.resetPassword' | translate" (onClick)="abrirRedefinirSenha(u)" />
                                <p-button
                                    [icon]="u.isActive ? 'pi pi-ban' : 'pi pi-check'"
                                    [text]="true"
                                    [rounded]="true"
                                    [severity]="u.isActive ? 'danger' : 'success'"
                                    [pTooltip]="(u.isActive ? 'users.list.tooltips.deactivate' : 'users.list.tooltips.reactivate') | translate"
                                    [disabled]="u.id === idLogado()"
                                    (onClick)="alternarAtivo(u)"
                                />
                            </td>
                        </tr>
                    </ng-template>
                </p-table>
            }
        </div>

        <!-- Diálogo: Novo usuário -->
        <p-dialog [(visible)]="cadastroAberto" [modal]="true" [closable]="true" [style]="{ width: '34rem' }" [header]="'users.form.createTitle' | translate">
            <div class="dialog-form">
                <div class="col-span-full">
                    <label class="campo-rotulo">{{ 'users.form.fullNameLabel' | translate }}</label>
                    <input pInputText [(ngModel)]="novo.fullName" class="w-full" maxlength="120" />
                </div>
                <div>
                    <label class="campo-rotulo">{{ 'users.form.emailLabel' | translate }}</label>
                    <input pInputText type="email" [(ngModel)]="novo.email" class="w-full" maxlength="180" />
                </div>
                <div>
                    <label class="campo-rotulo">{{ 'users.form.roleLabel' | translate }}</label>
                    <p-select [options]="papelOptions()" optionLabel="label" optionValue="value" [(ngModel)]="novo.role" appendTo="body" styleClass="w-full" />
                </div>
                <div class="col-span-full">
                    <label class="campo-rotulo">{{ 'users.form.passwordLabel' | translate }}</label>
                    <input pInputText type="password" [(ngModel)]="novo.password" class="w-full" maxlength="100" [placeholder]="'users.form.passwordPlaceholder' | translate" />
                </div>
            </div>
            <ng-template pTemplate="footer">
                <p-button [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" (onClick)="fecharCadastro()" />
                <p-button [label]="'common.actions.create' | translate" icon="pi pi-check" [loading]="salvando()" [disabled]="!novoEhValido() || salvando()" (onClick)="cadastrar()" />
            </ng-template>
        </p-dialog>

        <!-- Diálogo: Editar usuário -->
        <p-dialog [(visible)]="edicaoAberta" [modal]="true" [closable]="true" [style]="{ width: '34rem' }" [header]="'users.form.editTitle' | translate">
            @if (edicao(); as e) {
                <div class="dialog-form">
                    <div class="col-span-full">
                        <label class="campo-rotulo">{{ 'users.form.fullNameLabel' | translate }}</label>
                        <input pInputText [ngModel]="e.fullName" (ngModelChange)="atualizarEdicao('fullName', $event)" class="w-full" maxlength="120" />
                    </div>
                    <div>
                        <label class="campo-rotulo">{{ 'users.form.emailLabel' | translate }}</label>
                        <input pInputText [ngModel]="e.email" disabled class="w-full" />
                    </div>
                    <div>
                        <label class="campo-rotulo">{{ 'users.form.roleLabel' | translate }}</label>
                        <p-select [options]="papelOptions()" optionLabel="label" optionValue="value" [ngModel]="e.role" (ngModelChange)="atualizarEdicao('role', $event)" appendTo="body" styleClass="w-full" [disabled]="e.id === idLogado()" />
                        @if (e.id === idLogado()) {
                            <small class="text-muted-color">
                                {{ 'users.form.cannotChangeOwnRole' | translate }}
                            </small>
                        }
                    </div>
                </div>
            }
            <ng-template pTemplate="footer">
                <p-button [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" (onClick)="fecharEdicao()" />
                <p-button [label]="'common.actions.save' | translate" icon="pi pi-check" [loading]="salvando()" [disabled]="!edicaoEhValida() || salvando()" (onClick)="salvarEdicao()" />
            </ng-template>
        </p-dialog>

        <!-- Diálogo: Redefinir senha -->
        <p-dialog [(visible)]="senhaAberta" [modal]="true" [closable]="true" [style]="{ width: '30rem' }" [header]="'users.resetPassword.title' | translate">
            @if (redefinir(); as r) {
                <div class="dialog-form">
                    <div class="col-span-full">
                        <p class="m-0 mb-3 text-color-secondary" [innerHTML]="'users.resetPassword.description' | translate: { name: r.fullName }"></p>
                    </div>
                    <div class="col-span-full">
                        <label class="campo-rotulo">{{ 'users.resetPassword.newPasswordLabel' | translate }}</label>
                        <input pInputText type="password" [ngModel]="r.newPassword" (ngModelChange)="atualizarRedefinicao($event)" class="w-full" maxlength="100" [placeholder]="'users.form.passwordPlaceholder' | translate" />
                    </div>
                </div>
            }
            <ng-template pTemplate="footer">
                <p-button [label]="'common.actions.cancel' | translate" severity="secondary" [text]="true" (onClick)="fecharRedefinirSenha()" />
                <p-button [label]="'users.resetPassword.submit' | translate" icon="pi pi-key" [loading]="salvando()" [disabled]="!redefinicaoEhValida() || salvando()" (onClick)="salvarNovaSenha()" />
            </ng-template>
        </p-dialog>
    `,
    styles: [
        `
            .usuarios-toolbar {
                display: flex;
                gap: 0.75rem;
                margin-bottom: 1rem;
                flex-wrap: wrap;
                align-items: center;
            }
            .usuarios-busca {
                flex: 1 1 14rem;
                max-width: 24rem;
            }
            .usuarios-busca input {
                width: 100%;
            }
            .usuarios-carregando {
                display: flex;
                align-items: center;
                justify-content: center;
                padding: 2rem 0;
            }
            .dialog-form {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 1rem;
            }
            .col-span-full {
                grid-column: 1 / -1;
            }
            @media (max-width: 540px) {
                .dialog-form {
                    grid-template-columns: 1fr;
                }
            }
            .campo-rotulo {
                display: block;
                font-weight: 500;
                margin-bottom: 0.4rem;
                font-size: 0.88rem;
            }
        `
    ]
})
export class UsersPage {
    private readonly api = inject(UserService);
    private readonly auth = inject(AuthService);
    private readonly toast = inject(MessageService);
    private readonly confirm = inject(ConfirmationService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly papelOptions = computed<{ label: string; value: UserRole }[]>(() => {
        void this.language.atual();
        return [
            { value: 'Member', label: this.translate.instant('users.form.rolesOptions.Member') },
            { value: 'PavilionManager', label: this.translate.instant('users.form.rolesOptions.PavilionManager') },
            { value: 'Admin', label: this.translate.instant('users.form.rolesOptions.Admin') }
        ];
    });

    readonly filtrosPapel = computed<{ label: string; value: UserRole | null }[]>(() => {
        void this.language.atual();
        return [{ value: null, label: this.translate.instant('common.labels.allRoles') }, ...this.papelOptions()];
    });

    readonly filtrosStatus = computed<{ label: string; value: boolean | null }[]>(() => {
        void this.language.atual();
        return [
            { value: null, label: this.translate.instant('common.labels.allStatus') },
            { value: true, label: this.translate.instant('common.labels.onlyActive') },
            { value: false, label: this.translate.instant('common.labels.onlyInactive') }
        ];
    });

    readonly breadcrumb = computed(() => {
        void this.language.atual();
        return [{ label: this.translate.instant('menu.administration') }, { label: this.translate.instant('menu.users') }];
    });

    readonly usuarios = signal<UserListItem[]>([]);
    readonly carregando = signal(false);
    readonly salvando = signal(false);
    readonly cadastroAberto = signal(false);
    readonly edicaoAberta = signal(false);
    readonly senhaAberta = signal(false);

    readonly edicao = signal<EditarUsuarioForm | null>(null);
    readonly redefinir = signal<RedefinirSenhaForm | null>(null);

    readonly idLogado = computed(() => this.auth.currentUser()?.id ?? null);

    busca = '';
    papelFiltro: UserRole | null = null;
    statusFiltro: boolean | null = null;

    novo: NovoUsuarioForm = { fullName: '', email: '', role: 'Member', password: '' };

    private buscaTimer: ReturnType<typeof setTimeout> | null = null;

    constructor() {
        this.recarregar();
        // Re-renderiza tags de papel ao trocar idioma forçando refresh da lista.
        effect(() => {
            void this.language.atual();
            // intencionalmente sem chamada — basta para invalidar computeds que dependem
        });
    }

    agendarBusca(): void {
        if (this.buscaTimer) clearTimeout(this.buscaTimer);
        this.buscaTimer = setTimeout(() => this.recarregar(), 300);
    }

    recarregar(): void {
        this.carregando.set(true);
        this.api
            .list({
                search: this.busca.trim() || null,
                role: this.papelFiltro,
                isActive: this.statusFiltro
            })
            .subscribe({
                next: (lista) => {
                    this.usuarios.set(lista);
                    this.carregando.set(false);
                },
                error: () => {
                    this.carregando.set(false);
                    this.toast.add({
                        severity: 'error',
                        summary: this.translate.instant('common.labels.error'),
                        detail: this.translate.instant('users.toasts.errorLoad')
                    });
                }
            });
    }

    abrirCadastro(): void {
        this.novo = { fullName: '', email: '', role: 'Member', password: '' };
        this.cadastroAberto.set(true);
    }

    fecharCadastro(): void {
        this.cadastroAberto.set(false);
    }

    novoEhValido(): boolean {
        return this.novo.fullName.trim().length >= 3 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.novo.email.trim()) && this.novo.password.length >= 8;
    }

    cadastrar(): void {
        if (!this.novoEhValido()) return;
        this.salvando.set(true);
        this.api
            .create({
                fullName: this.novo.fullName.trim(),
                email: this.novo.email.trim(),
                password: this.novo.password,
                role: this.novo.role
            })
            .subscribe({
                next: () => {
                    this.salvando.set(false);
                    this.toast.add({
                        severity: 'success',
                        summary: this.translate.instant('users.toasts.created.summary'),
                        detail: this.translate.instant('users.toasts.created.detail')
                    });
                    this.fecharCadastro();
                    this.recarregar();
                },
                error: (err) => {
                    this.salvando.set(false);
                    this.toast.add({
                        severity: 'error',
                        summary: this.translate.instant('common.labels.error'),
                        detail: this.mensagemErro(err, 'users.toasts.errorCreate')
                    });
                }
            });
    }

    abrirEdicao(u: UserListItem): void {
        this.edicao.set({ id: u.id, fullName: u.fullName, email: u.email, role: u.role });
        this.edicaoAberta.set(true);
    }

    fecharEdicao(): void {
        this.edicaoAberta.set(false);
        this.edicao.set(null);
    }

    atualizarEdicao<K extends keyof EditarUsuarioForm>(campo: K, valor: EditarUsuarioForm[K]): void {
        const atual = this.edicao();
        if (!atual) return;
        this.edicao.set({ ...atual, [campo]: valor });
    }

    edicaoEhValida(): boolean {
        const e = this.edicao();
        return !!e && e.fullName.trim().length >= 3;
    }

    salvarEdicao(): void {
        const e = this.edicao();
        if (!e || !this.edicaoEhValida()) return;
        this.salvando.set(true);
        this.api.update(e.id, { fullName: e.fullName.trim(), role: e.role }).subscribe({
            next: () => {
                this.salvando.set(false);
                this.toast.add({
                    severity: 'success',
                    summary: this.translate.instant('users.toasts.updated.summary'),
                    detail: this.translate.instant('users.toasts.updated.detail')
                });
                this.fecharEdicao();
                this.recarregar();
            },
            error: (err) => {
                this.salvando.set(false);
                this.toast.add({
                    severity: 'error',
                    summary: this.translate.instant('common.labels.error'),
                    detail: this.mensagemErro(err, 'users.toasts.errorUpdate')
                });
            }
        });
    }

    abrirRedefinirSenha(u: UserListItem): void {
        this.redefinir.set({ id: u.id, fullName: u.fullName, newPassword: '' });
        this.senhaAberta.set(true);
    }

    fecharRedefinirSenha(): void {
        this.senhaAberta.set(false);
        this.redefinir.set(null);
    }

    atualizarRedefinicao(valor: string): void {
        const atual = this.redefinir();
        if (!atual) return;
        this.redefinir.set({ ...atual, newPassword: valor });
    }

    redefinicaoEhValida(): boolean {
        const r = this.redefinir();
        return !!r && r.newPassword.length >= 8;
    }

    salvarNovaSenha(): void {
        const r = this.redefinir();
        if (!r || !this.redefinicaoEhValida()) return;
        this.salvando.set(true);
        this.api.resetPassword(r.id, { newPassword: r.newPassword }).subscribe({
            next: () => {
                this.salvando.set(false);
                this.toast.add({
                    severity: 'success',
                    summary: this.translate.instant('users.toasts.passwordReset.summary'),
                    detail: this.translate.instant('users.toasts.passwordReset.detail')
                });
                this.fecharRedefinirSenha();
            },
            error: (err) => {
                this.salvando.set(false);
                this.toast.add({
                    severity: 'error',
                    summary: this.translate.instant('common.labels.error'),
                    detail: this.mensagemErro(err, 'users.toasts.errorReset')
                });
            }
        });
    }

    alternarAtivo(u: UserListItem): void {
        if (u.id === this.idLogado()) return;
        const desativando = u.isActive;
        this.confirm.confirm({
            header: this.translate.instant(desativando ? 'users.confirmDeactivate.title' : 'users.confirmReactivate.title'),
            message: this.translate.instant(desativando ? 'users.confirmDeactivate.message' : 'users.confirmReactivate.message', { name: u.fullName }),
            acceptLabel: this.translate.instant('common.actions.confirm'),
            rejectLabel: this.translate.instant('common.actions.cancel'),
            acceptIcon: desativando ? 'pi pi-ban' : 'pi pi-check',
            acceptButtonStyleClass: desativando ? 'p-button-danger' : 'p-button-success',
            accept: () => {
                const obs = desativando ? this.api.deactivate(u.id) : this.api.activate(u.id);
                obs.subscribe({
                    next: () => {
                        const chave = desativando ? 'users.toasts.deactivated' : 'users.toasts.reactivated';
                        this.toast.add({
                            severity: 'success',
                            summary: this.translate.instant(`${chave}.summary`),
                            detail: this.translate.instant(`${chave}.detail`, { name: u.fullName })
                        });
                        this.recarregar();
                    },
                    error: (err) => {
                        this.toast.add({
                            severity: 'error',
                            summary: this.translate.instant('common.labels.error'),
                            detail: this.mensagemErro(err, 'users.toasts.errorToggle')
                        });
                    }
                });
            }
        });
    }

    rotuloPapel(r: UserRole): string {
        return this.translate.instant(`users.form.rolesOptions.${r}`);
    }

    severidadePapel(r: UserRole): 'success' | 'info' | 'warn' | 'secondary' {
        if (r === 'Admin') return 'warn';
        if (r === 'PavilionManager') return 'info';
        return 'secondary';
    }

    private mensagemErro(err: unknown, fallbackKey: string): string {
        const e = err as { error?: { detail?: string; title?: string; message?: string } } | null;
        return e?.error?.detail || e?.error?.title || e?.error?.message || this.translate.instant(fallbackKey);
    }
}
