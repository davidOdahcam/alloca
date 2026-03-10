import { Component, input } from '@angular/core';
import { RouterModule } from '@angular/router';

export interface HeroBreadcrumb {
    label: string;
    rota?: string;
}

@Component({
    selector: 'app-page-hero',
    standalone: true,
    imports: [RouterModule],
    template: `
        <section class="page-hero" [class.page-hero--compacto]="compacto()">
            @if (breadcrumb().length > 0) {
                <nav class="page-hero__trilha" aria-label="Navegação estrutural">
                    <i class="pi pi-home page-hero__trilha-home" aria-hidden="true"></i>
                    @for (item of breadcrumb(); track item.label; let last = $last) {
                        <i class="pi pi-angle-right page-hero__trilha-sep" aria-hidden="true"></i>
                        @if (item.rota && !last) {
                            <a [routerLink]="item.rota" class="page-hero__trilha-link">{{ item.label }}</a>
                        } @else {
                            <span class="page-hero__trilha-atual" [attr.aria-current]="last ? 'page' : null">{{ item.label }}</span>
                        }
                    }
                </nav>
            }

            <div class="page-hero__corpo">
                @if (icone()) {
                    <span class="page-hero__icone-wrap" aria-hidden="true">
                        <i [class]="'pi ' + icone()"></i>
                    </span>
                }
                <div class="page-hero__texto">
                    <h1 class="page-hero__titulo">{{ title() }}</h1>
                    @if (description()) {
                        <p class="page-hero__desc">{{ description() }}</p>
                    }
                </div>
                <div class="page-hero__acoes">
                    <ng-content />
                </div>
            </div>
        </section>
    `,
    styles: [
        `
            .page-hero {
                position: relative;
                background: var(--surface-card);
                border: 1px solid var(--surface-border);
                border-radius: 0.875rem;
                padding: 1.35rem 1.5rem 1.2rem;
                margin-bottom: 1.25rem;
                display: flex;
                flex-direction: column;
                gap: 0.7rem;
                overflow: hidden;
            }
            /* Barra de acento superior */
            .page-hero::before {
                content: '';
                position: absolute;
                inset: 0 0 auto 0;
                height: 3px;
                background: linear-gradient(90deg, var(--primary-color), color-mix(in srgb, var(--primary-color), transparent 60%));
                border-radius: 0.875rem 0.875rem 0 0;
            }
            /* Halo decorativo no canto */
            .page-hero::after {
                content: '';
                position: absolute;
                top: -3rem;
                right: -3rem;
                width: 10rem;
                height: 10rem;
                border-radius: 50%;
                background: color-mix(in srgb, var(--primary-color), transparent 93%);
                pointer-events: none;
            }

            /* Variante compacta */
            .page-hero--compacto {
                padding: 0.9rem 1.25rem;
                gap: 0.4rem;
                margin-bottom: 1rem;
            }
            .page-hero--compacto .page-hero__titulo {
                font-size: 1.25rem;
            }

            /* Trilha (breadcrumb) */
            .page-hero__trilha {
                display: flex;
                align-items: center;
                gap: 0.3rem;
                flex-wrap: wrap;
                font-size: 0.78rem;
                color: var(--text-color-secondary);
                position: relative;
                z-index: 1;
            }
            .page-hero__trilha-home {
                font-size: 0.75rem;
                opacity: 0.7;
            }
            .page-hero__trilha-sep {
                font-size: 0.65rem;
                opacity: 0.45;
            }
            .page-hero__trilha-link {
                color: var(--primary-color);
                text-decoration: none;
                opacity: 0.85;
                transition: opacity 0.15s;
            }
            .page-hero__trilha-link:hover {
                opacity: 1;
                text-decoration: underline;
            }
            .page-hero__trilha-atual {
                font-weight: 500;
                color: var(--text-color);
            }

            /* Corpo */
            .page-hero__corpo {
                display: flex;
                align-items: center;
                gap: 1rem;
                flex-wrap: wrap;
                position: relative;
                z-index: 1;
            }

            /* Ícone */
            .page-hero__icone-wrap {
                flex-shrink: 0;
                width: 2.75rem;
                height: 2.75rem;
                border-radius: 0.75rem;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                background: color-mix(in srgb, var(--primary-color), transparent 88%);
                color: var(--primary-color);
                font-size: 1.2rem;
                box-shadow: 0 0 0 4px color-mix(in srgb, var(--primary-color), transparent 94%);
            }

            /* Texto */
            .page-hero__texto {
                display: flex;
                flex-direction: column;
                gap: 0.2rem;
                flex: 1;
                min-width: 0;
            }
            .page-hero__titulo {
                font-size: 1.55rem;
                font-weight: 700;
                margin: 0;
                line-height: 1.2;
                color: var(--text-color);
                letter-spacing: -0.02em;
            }
            .page-hero__desc {
                margin: 0;
                color: var(--text-color-secondary);
                font-size: 0.9rem;
                line-height: 1.4;
            }

            /* Ações */
            .page-hero__acoes {
                display: flex;
                gap: 0.5rem;
                flex-wrap: wrap;
                align-items: center;
                margin-left: auto;
            }
        `
    ]
})
export class PageHero {
    readonly title = input.required<string>();
    readonly description = input<string | null>(null);
    readonly icone = input<string>('');
    readonly breadcrumb = input<HeroBreadcrumb[]>([]);
    readonly compacto = input<boolean>(false);
}
