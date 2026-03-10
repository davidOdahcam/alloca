import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
    selector: 'app-empty-state',
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="empty-state" [class.empty-state--compact]="compact()">
            @if (icone()) {
                <i class="empty-state__icone" [class]="icone()"></i>
            }
            <div class="empty-state__titulo">{{ titulo() }}</div>
            @if (descricao()) {
                <p class="empty-state__desc">{{ descricao() }}</p>
            }
            <div class="empty-state__acoes">
                <ng-content />
            </div>
        </div>
    `,
    styles: [
        `
            .empty-state {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 0.4rem;
                padding: 2.5rem 1rem;
                text-align: center;
                color: var(--text-color-secondary);
            }
            .empty-state--compact {
                padding: 1.25rem 0.75rem;
            }
            .empty-state__icone {
                font-size: 1.85rem;
                margin-bottom: 0.4rem;
                color: var(--text-color-secondary);
                opacity: 0.7;
            }
            .empty-state__titulo {
                font-weight: 600;
                font-size: 1rem;
                color: var(--text-color);
            }
            .empty-state__desc {
                margin: 0;
                font-size: 0.85rem;
                max-width: 32rem;
            }
            .empty-state__acoes {
                display: flex;
                gap: 0.5rem;
                flex-wrap: wrap;
                justify-content: center;
                margin-top: 0.5rem;
            }
            .empty-state__acoes:empty {
                display: none;
            }
        `
    ]
})
export class EmptyState {
    readonly icone = input<string>('pi pi-inbox');
    readonly titulo = input.required<string>();
    readonly descricao = input<string>('');
    readonly compact = input<boolean>(false);
}
