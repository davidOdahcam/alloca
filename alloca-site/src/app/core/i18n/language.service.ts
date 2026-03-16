import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';

export type IdiomaSuportado = 'pt-BR' | 'en-US';

const STORAGE_KEY = 'app:language';
const IDIOMAS: IdiomaSuportado[] = ['pt-BR', 'en-US'];
const PADRAO: IdiomaSuportado = 'pt-BR';

/**
 * Leitura pura do idioma atual a partir de `localStorage` (com fallback para
 * o idioma do navegador). Não depende do Angular nem do `TranslateService`,
 * permitindo que interceptors HTTP descubram o idioma sem participar do
 * grafo de DI do `LanguageService` (evita NG0200 circular dependency).
 */
export function lerIdiomaAtual(): IdiomaSuportado {
    try {
        const salvo = localStorage.getItem(STORAGE_KEY) as IdiomaSuportado | null;
        if (salvo && IDIOMAS.includes(salvo)) return salvo;
    } catch {
        /* ignora */
    }
    const navegador = (typeof navigator !== 'undefined' && navigator.language) || PADRAO;
    const correspondente = IDIOMAS.find((i) => navegador.toLowerCase().startsWith(i.toLowerCase().split('-')[0]));
    return correspondente ?? PADRAO;
}

@Injectable({ providedIn: 'root' })
export class LanguageService {
    private readonly translate = inject(TranslateService);
    private readonly primeng = inject(PrimeNG);

    private readonly _atual = signal<IdiomaSuportado>(lerIdiomaAtual());
    readonly atual = this._atual.asReadonly();
    readonly disponiveis = computed(() => IDIOMAS);

    constructor() {
        // Aplica idioma inicial e mantém PrimeNG sincronizado.
        effect(() => {
            const idioma = this._atual();
            this.translate.use(idioma).subscribe(() => {
                const primengTranslation = this.translate.instant('primeng');
                if (primengTranslation && typeof primengTranslation === 'object') {
                    this.primeng.setTranslation(primengTranslation);
                }
                document.documentElement.lang = idioma;
            });
        });
    }

    definir(idioma: IdiomaSuportado): void {
        if (!IDIOMAS.includes(idioma)) return;
        this._atual.set(idioma);
        try {
            localStorage.setItem(STORAGE_KEY, idioma);
        } catch {
            /* ignora */
        }
    }

    private detectarInicial(): IdiomaSuportado {
        return lerIdiomaAtual();
    }
}
