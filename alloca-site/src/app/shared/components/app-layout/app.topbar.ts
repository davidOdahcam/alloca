import { Component, computed, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { PopoverModule, Popover } from 'primeng/popover';
import { TooltipModule } from 'primeng/tooltip';
import { StyleClassModule } from 'primeng/styleclass';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { SelectModule } from 'primeng/select';
import { LayoutService } from '@core/layout/layout.service';
import { AuthService } from '@core/auth/auth.service';
import { LanguageService, IdiomaSuportado } from '@core/i18n/language.service';
import { UserRole } from '@core/auth/models/auth.model';

const ROLE_LABEL_KEY: Record<UserRole, string> = {
    Admin: 'roles.admin',
    PavilionManager: 'roles.manager',
    Member: 'roles.member'
};

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, FormsModule, TranslatePipe, StyleClassModule, ButtonModule, PopoverModule, TooltipModule, ToggleSwitchModule, SelectModule],
    template: ` <div class="layout-topbar">
        <div class="layout-topbar-logo-container">
            <button class="layout-menu-button layout-topbar-action" (click)="layoutService.onMenuToggle()" [attr.aria-label]="'menu.toggle' | translate">
                <i class="pi pi-bars"></i>
            </button>
            <a class="layout-topbar-logo" routerLink="/">
                <i class="pi pi-objects-column" style="font-size: 1.6rem; color: var(--primary-color);"></i>
                <span>ALLOCA</span>
            </a>
        </div>

        <div class="layout-topbar-actions">
            <button type="button" class="layout-topbar-action layout-topbar-user" (click)="userPopover.toggle($event)" aria-haspopup="true" [attr.aria-label]="'topbar.userMenu' | translate">
                <span class="topbar-avatar" [attr.title]="userLabel()">{{ userInitials() }}</span>
                <span class="topbar-user-info hidden md:flex">
                    <span class="topbar-user-info__nome">{{ userLabel() }}</span>
                    <span class="topbar-user-info__papel">{{ userRoleLabel() | translate }}</span>
                </span>
                <i class="pi pi-user topbar-user-caret hidden md:inline"></i>
            </button>

            <p-popover #userPopover appendTo="body" styleClass="topbar-user-popover">
                <div class="menu-conta">
                    <div class="menu-conta__cabecalho">
                        <span class="topbar-avatar topbar-avatar--grande">{{ userInitials() }}</span>
                        <div class="menu-conta__id">
                            <strong>{{ userLabel() }}</strong>
                            <span class="menu-conta__email">{{ userEmail() }}</span>
                            @if (userRoleLabel()) {
                                <span class="menu-conta__papel">
                                    <i class="pi pi-shield"></i>
                                    {{ userRoleLabel() | translate }}
                                </span>
                            }
                        </div>
                    </div>

                    <div class="menu-conta__secao">
                        <span class="menu-conta__titulo">{{ 'topbar.preferences' | translate }}</span>
                        <div class="menu-conta__pref">
                            <span class="menu-conta__pref-icone">
                                <i [class]="layoutService.isDarkTheme() ? 'pi pi-moon' : 'pi pi-sun'"></i>
                            </span>
                            <div class="menu-conta__pref-texto">
                                <span class="menu-conta__pref-rotulo">{{ 'topbar.darkTheme.label' | translate }}</span>
                                <span class="menu-conta__pref-descricao">{{ 'topbar.darkTheme.description' | translate }}</span>
                            </div>
                            <p-toggleswitch [ngModel]="layoutService.isDarkTheme()" (ngModelChange)="definirTema($event)" [ariaLabel]="'topbar.darkTheme.label' | translate" />
                        </div>

                        <div class="menu-conta__pref menu-conta__pref--coluna">
                            <div class="menu-conta__pref-cabecalho">
                                <span class="menu-conta__pref-icone">
                                    <i class="pi pi-globe"></i>
                                </span>
                                <div class="menu-conta__pref-texto">
                                    <span class="menu-conta__pref-rotulo">{{ 'language.label' | translate }}</span>
                                    <span class="menu-conta__pref-descricao">{{ 'topbar.language.description' | translate }}</span>
                                </div>
                            </div>
                            <p-select [options]="opcoesIdioma" optionLabel="label" optionValue="value" [ngModel]="idiomaAtual()" (ngModelChange)="alterarIdioma($event)" appendTo="body" styleClass="topbar-language-select" />
                        </div>
                    </div>

                    <div class="menu-conta__rodape">
                        <p-button [label]="'menu.logout' | translate" icon="pi pi-sign-out" severity="danger" [outlined]="true" styleClass="w-full" (onClick)="sair(userPopover)" />
                    </div>
                </div>
            </p-popover>
        </div>
    </div>`,
    styles: [
        `
            .layout-topbar-user {
                display: inline-flex;
                align-items: center;
                gap: 0.55rem;
                padding: 0.25rem 0.5rem 0.25rem 0.25rem;
                border-radius: 999px;
            }
            .topbar-avatar {
                width: 2rem;
                height: 2rem;
                border-radius: 999px;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                font-size: 0.78rem;
                font-weight: 600;
                background: color-mix(in srgb, var(--primary-color), transparent 80%);
                color: var(--primary-color);
                text-transform: uppercase;
                letter-spacing: 0.02em;
            }
            .topbar-user-info {
                flex-direction: column;
                align-items: flex-start;
                line-height: 1.1;
                gap: 0.1rem;
            }
            .topbar-user-info__nome {
                font-size: 0.85rem;
                font-weight: 600;
                color: var(--text-color);
                max-width: 12rem;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
            .topbar-user-info__papel {
                font-size: 0.7rem;
                color: var(--text-color-secondary);
                text-transform: uppercase;
                letter-spacing: 0.04em;
            }
            .topbar-user-caret {
                font-size: 0.7rem;
                color: var(--text-color-secondary);
            }
            ::ng-deep .topbar-language-select {
                width: 100%;
                min-width: 0;
            }
        `
    ]
})
export class AppTopbar {
    layoutService = inject(LayoutService);
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly language = inject(LanguageService);
    private readonly translate = inject(TranslateService);

    readonly userLabel = computed(() => this.auth.currentUser()?.fullName ?? this.translate.instant('topbar.visitor'));
    readonly userEmail = computed(() => this.auth.currentUser()?.email ?? '');

    readonly userInitials = computed(() => {
        const name = this.auth.currentUser()?.fullName ?? '';
        if (!name) return '?';
        const parts = name.trim().split(/\s+/);
        const first = parts[0]?.[0] ?? '';
        const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
        return (first + last).toUpperCase();
    });

    readonly userRoleLabel = computed(() => {
        const role = this.auth.role();
        return role ? ROLE_LABEL_KEY[role] : '';
    });

    readonly idiomaAtual = computed(() => this.language.atual());

    readonly opcoesIdioma: { label: string; value: IdiomaSuportado }[] = [
        { label: 'Português (Brasil)', value: 'pt-BR' }
    ];

    definirTema(escuro: boolean): void {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: escuro }));
    }

    alterarIdioma(idioma: IdiomaSuportado): void {
        this.language.definir(idioma);
    }

    sair(popover: Popover): void {
        popover.hide();
        this.auth.logout();
        this.router.navigate(['/auth/login']);
    }
}
