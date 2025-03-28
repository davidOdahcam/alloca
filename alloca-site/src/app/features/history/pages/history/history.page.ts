import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MessageModule } from 'primeng/message';
import { PageHero } from '@shared/components/page-hero/page-hero';

@Component({
    selector: 'app-manager-history',
    standalone: true,
    imports: [MessageModule, PageHero, TranslatePipe],
    template: `
        <app-page-hero [title]="'history.title' | translate" [description]="'history.subtitle' | translate" [breadcrumb]="[{ label: ('menu.operations' | translate) }, { label: ('history.title' | translate) }]" />

        <div class="card">
            <p-message severity="info" [text]="'history.empty' | translate" styleClass="w-full" />
        </div>
    `
})
export class HistoryPage {}
