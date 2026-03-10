import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-loader',
    standalone: true,
    imports: [CommonModule, ProgressSpinnerModule],
    template: `
        <div class="loader" [class.loader--inline]="inline()">
            <p-progressSpinner [strokeWidth]="strokeWidth()" [style]="{ width: tamanho(), height: tamanho() }" />
            @if (rotulo()) {
                <span class="loader__rotulo">{{ rotulo() }}</span>
            }
        </div>
    `,
    styles: [
        `
            .loader {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 0.6rem;
                padding: 1.5rem 0.75rem;
            }
            .loader--inline {
                flex-direction: row;
                padding: 0.5rem;
            }
            .loader__rotulo {
                font-size: 0.85rem;
                color: var(--text-color-secondary);
            }
        `
    ]
})
export class Loader {
    readonly rotulo = input<string>('');
    readonly tamanho = input<string>('2.5rem');
    readonly strokeWidth = input<string>('4');
    readonly inline = input<boolean>(false);
}
