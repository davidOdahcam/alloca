import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { AuthService } from '@core/auth/auth.service';
import { LanguageService } from '@core/i18n/language.service';
import { environment } from '@env/environment';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        @for (item of model(); track item.label) {
            @if (!item.separator) {
                <li app-menuitem [item]="item" [root]="true"></li>
            } @else {
                <li class="menu-separator"></li>
            }
        }
    </ul> `
})
export class AppMenu {
    private readonly auth = inject(AuthService);
    private readonly translate = inject(TranslateService);
    private readonly language = inject(LanguageService);

    readonly model = computed<MenuItem[]>(() => {
        void this.language.atual();
        const role = this.auth.role();
        if (role === 'PavilionManager' || role === 'Admin') return this.managerMenu(role === 'Admin');
        if (role === 'Member') return this.studentMenu();
        return [];
    });

    private t(key: string): string {
        return this.translate.instant(key);
    }

    private studentMenu(): MenuItem[] {
        return [
            {
                label: this.t('menu.reservations'),
                items: [
                    { label: this.t('menu.newReservation'), icon: 'pi pi-fw pi-calendar-plus', routerLink: ['/student/reserve'] },
                    {
                        label: this.t('menu.myReservations'),
                        icon: 'pi pi-fw pi-list',
                        routerLink: ['/student/reservations']
                    }
                ]
            }
        ];
    }

    private managerMenu(isAdmin: boolean): MenuItem[] {
        const operacao: MenuItem[] = [
            { label: this.t('menu.dashboard'), icon: 'pi pi-fw pi-chart-bar', routerLink: ['/manager'] },
            {
                label: this.t('menu.approvals'),
                icon: 'pi pi-fw pi-inbox',
                routerLink: ['/manager/approvals']
            },
            { label: this.t('menu.blocks'), icon: 'pi pi-fw pi-ban', routerLink: ['/manager/blocks'] }
        ];

        if (environment.features.managerHistory) {
            operacao.push({ label: this.t('menu.history'), icon: 'pi pi-fw pi-history', routerLink: ['/manager/history'] });
        }

        const grupos: MenuItem[] = [{ label: this.t('menu.operations'), items: operacao }];

        if (isAdmin && environment.features.managerUsers) {
            grupos.push({
                label: this.t('menu.administration'),
                items: [{ label: this.t('menu.users'), icon: 'pi pi-fw pi-users', routerLink: ['/manager/users'] }]
            });
        }
        return grupos;
    }
}
