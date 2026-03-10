import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AfterViewInit, ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

export interface FloorMapViewport {
    x: number;
    y: number;
    scale: number;
}

/** Recurso interativo do mapa. Estruturalmente compatível com AvailabilityResource. */
export interface RecursoMapa {
    id: string;
    externalId: string;
    name: string;
    type: 'Room' | 'Desk';
    available: boolean;
}

@Component({
    selector: 'app-floor-map',
    standalone: true,
    imports: [CommonModule, ButtonModule, TooltipModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './floor-map.component.html',
    styleUrl: './floor-map.component.scss',
    host: {
        '(keydown)': 'onKeyDown($event)'
    }
})
export class FloorMapComponent implements OnInit, AfterViewInit {
    private readonly http = inject(HttpClient);
    private readonly sanitizer = inject(DomSanitizer);
    private readonly destroyRef = inject(DestroyRef);

    readonly svgUrl = input<string>();
    readonly svgMarkup = input<string>();
    readonly ariaLabel = input<string>('Mapa interativo do pavimento');
    readonly minScale = input<number>(0.5);
    readonly maxScale = input<number>(4);
    readonly zoomStep = input<number>(0.2);
    readonly panStep = input<number>(40);

    /** Recursos exibidos no mapa. Cada um casa pelo externalId com data-identification do SVG. */
    readonly recursos = input<readonly RecursoMapa[]>([]);
    /** Se true, recursos não listados em `recursos` ficam ocultos/inertes. */
    readonly esconderForaDaLista = input<boolean>(false);

    readonly viewportChange = output<FloorMapViewport>();
    /** Emite quando o usuário clica/aciona um recurso disponível. */
    readonly recursoSelecionado = output<RecursoMapa>();
    /** Emite quando o usuário clica num recurso marcado como indisponível. */
    readonly recursoIndisponivelSelecionado = output<RecursoMapa>();

    private readonly viewportRef = viewChild.required<ElementRef<HTMLDivElement>>('viewport');
    private readonly stageRef = viewChild.required<ElementRef<HTMLDivElement>>('stage');

    protected readonly scale = signal(1);
    protected readonly tx = signal(0);
    protected readonly ty = signal(0);
    protected readonly svgHtml = signal<SafeHtml | null>(null);
    protected readonly isPanning = signal(false);

    protected readonly transform = computed(() => `translate(${this.tx()}px, ${this.ty()}px) scale(${this.scale()})`);

    protected readonly zoomPercent = computed(() => Math.round(this.scale() * 100));
    protected readonly canZoomIn = computed(() => this.scale() < this.maxScale());
    protected readonly canZoomOut = computed(() => this.scale() > this.minScale());

    // Tooltip flutuante para hover em recursos
    protected readonly tipVisivel = signal(false);
    protected readonly tipTitulo = signal('');
    protected readonly tipCodigo = signal('');
    protected readonly tipDisponivel = signal(true);
    protected readonly tipX = signal(0);
    protected readonly tipY = signal(0);

    private dragStart: { x: number; y: number; tx: number; ty: number } | null = null;
    /** Evita disparar click depois de um pan. */
    private podeClicar = true;

    constructor() {
        effect(() => {
            this.viewportChange.emit({ x: this.tx(), y: this.ty(), scale: this.scale() });
        });

        // Reaplica o estado interativo sempre que `recursos` mudar OU o SVG for trocado.
        effect(() => {
            this.svgHtml();
            this.recursos();
            queueMicrotask(() => this.aplicarInteratividade());
        });
    }

    ngOnInit(): void {
        const markup = this.svgMarkup();
        const url = this.svgUrl();
        if (markup) {
            this.setSvg(markup);
            return;
        }
        if (url) {
            this.http
                .get(url, { responseType: 'text' })
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe((m) => this.setSvg(m));
        }
    }

    ngAfterViewInit(): void {
        this.aplicarInteratividade();
    }

    private setSvg(markup: string): void {
        this.svgHtml.set(this.sanitizer.bypassSecurityTrustHtml(markup));
    }

    /** Liga classes, aria, listeners para cada <g data-component="ROOM|DESK"> do SVG. */
    private aplicarInteratividade(): void {
        const stage = this.stageRef()?.nativeElement;
        if (!stage) return;
        const svg = stage.querySelector('svg');
        if (!svg) return;

        // Listeners delegados são amarrados uma única vez por SVG; assim, mudanças
        // de disponibilidade são sempre lidas do signal atual (sem closure stale).
        this.ligarListenersDelegados(svg);

        const mapa = new Map(this.recursos().map((r) => [r.externalId.toUpperCase(), r]));
        const grupos = svg.querySelectorAll<SVGGElement>('[data-component="ROOM"], [data-component="DESK"]');

        grupos.forEach((g) => {
            const tipo = g.getAttribute('data-component') === 'ROOM' ? 'Room' : 'Desk';
            const ext = (g.getAttribute('data-identification') ?? '').toUpperCase();
            const recurso = mapa.get(`${tipo.toUpperCase()}-${ext}`);
            const baseClass = tipo === 'Room' ? 'room' : 'desk';

            // Limpa estados anteriores.
            g.classList.remove(`${baseClass}--available`, `${baseClass}--indisponivel`, `${baseClass}--disabled`);

            if (!recurso) {
                if (this.esconderForaDaLista()) g.classList.add(`${baseClass}--disabled`);
                this.tornarInerte(g);
                return;
            }

            if (recurso.available) {
                g.classList.add(`${baseClass}--available`);
                g.setAttribute('role', 'button');
                g.setAttribute('tabindex', '0');
                g.setAttribute('aria-label', `${recurso.name} (${recurso.externalId}) — disponível`);
            } else {
                g.classList.add(`${baseClass}--indisponivel`);
                g.setAttribute('role', 'button');
                g.setAttribute('aria-disabled', 'true');
                g.setAttribute('aria-label', `${recurso.name} (${recurso.externalId}) — indisponível`);
                this.tornarInerte(g);
            }
        });
    }

    /**
     * Amarra os handlers de clique/teclado/hover uma única vez por elemento SVG.
     * Toda lookup do recurso é feita no momento do evento contra `this.recursos()`,
     * evitando o bug de closure que mantinha o estado de disponibilidade defasado.
     */
    private ligarListenersDelegados(svg: SVGSVGElement): void {
        if ((svg as any).__allocaDelegated) return;
        (svg as any).__allocaDelegated = true;

        const buscarRecurso = (target: EventTarget | null): RecursoMapa | undefined => {
            const el = (target as Element | null)?.closest?.('[data-component="ROOM"], [data-component="DESK"]') as SVGGElement | null;
            if (!el) return undefined;
            const tipo = el.getAttribute('data-component') === 'ROOM' ? 'Room' : 'Desk';
            const ext = (el.getAttribute('data-identification') ?? '').toUpperCase();
            const chave = `${tipo.toUpperCase()}-${ext}`;
            return this.recursos().find((r) => r.externalId.toUpperCase() === chave);
        };

        svg.addEventListener('click', (ev) => {
            const recurso = buscarRecurso(ev.target);
            if (!recurso) return;
            ev.stopPropagation();
            if (!this.podeClicar) return;
            if (recurso.available) {
                this.recursoSelecionado.emit(recurso);
            } else {
                this.recursoIndisponivelSelecionado.emit(recurso);
            }
        });

        svg.addEventListener('keydown', (ev: KeyboardEvent) => {
            if (ev.key !== 'Enter' && ev.key !== ' ') return;
            const recurso = buscarRecurso(ev.target);
            if (!recurso) return;
            ev.preventDefault();
            if (recurso.available) {
                this.recursoSelecionado.emit(recurso);
            } else {
                this.recursoIndisponivelSelecionado.emit(recurso);
            }
        });

        const mostrarTooltip = (ev: MouseEvent) => {
            const recurso = buscarRecurso(ev.target);
            if (!recurso) {
                this.tipVisivel.set(false);
                return;
            }
            const rect = this.viewportRef().nativeElement.getBoundingClientRect();
            this.tipTitulo.set(recurso.name);
            this.tipCodigo.set(recurso.externalId);
            this.tipDisponivel.set(recurso.available);
            this.tipX.set(ev.clientX - rect.left + 12);
            this.tipY.set(ev.clientY - rect.top + 12);
            this.tipVisivel.set(true);
        };
        svg.addEventListener('mousemove', mostrarTooltip);
        svg.addEventListener('mouseleave', () => this.tipVisivel.set(false));
    }

    private tornarInerte(g: SVGGElement): void {
        g.removeAttribute('tabindex');
    }

    zoomIn(): void {
        this.applyZoom(this.scale() + this.zoomStep());
    }
    zoomOut(): void {
        this.applyZoom(this.scale() - this.zoomStep());
    }
    reset(): void {
        this.scale.set(1);
        this.tx.set(0);
        this.ty.set(0);
    }

    private applyZoom(next: number, originX?: number, originY?: number): void {
        const clamped = Math.min(this.maxScale(), Math.max(this.minScale(), next));
        if (clamped === this.scale()) return;
        if (originX !== undefined && originY !== undefined) {
            const ratio = clamped / this.scale();
            this.tx.set(originX - (originX - this.tx()) * ratio);
            this.ty.set(originY - (originY - this.ty()) * ratio);
        }
        this.scale.set(clamped);
    }

    onWheel(event: WheelEvent): void {
        event.preventDefault();
        const rect = this.viewportRef().nativeElement.getBoundingClientRect();
        const ox = event.clientX - rect.left;
        const oy = event.clientY - rect.top;
        const direction = event.deltaY < 0 ? 1 : -1;
        this.applyZoom(this.scale() + direction * this.zoomStep(), ox, oy);
    }

    panBy(dx: number, dy: number): void {
        this.tx.update((v) => v + dx);
        this.ty.update((v) => v + dy);
    }

    onPointerDown(event: PointerEvent): void {
        if (event.button !== 0) return;
        (event.target as Element).setPointerCapture?.(event.pointerId);
        this.isPanning.set(true);
        this.podeClicar = true;
        this.tipVisivel.set(false);
        this.dragStart = { x: event.clientX, y: event.clientY, tx: this.tx(), ty: this.ty() };
    }

    onPointerMove(event: PointerEvent): void {
        if (!this.isPanning() || !this.dragStart) return;
        const dx = event.clientX - this.dragStart.x;
        const dy = event.clientY - this.dragStart.y;
        if (Math.abs(dx) + Math.abs(dy) > 4) this.podeClicar = false;
        this.tx.set(this.dragStart.tx + dx);
        this.ty.set(this.dragStart.ty + dy);
    }

    onPointerUp(event: PointerEvent): void {
        (event.target as Element).releasePointerCapture?.(event.pointerId);
        this.isPanning.set(false);
        this.dragStart = null;
    }

    onKeyDown(event: KeyboardEvent): void {
        const step = this.panStep();
        switch (event.key) {
            case 'ArrowUp':
                this.panBy(0, step);
                event.preventDefault();
                break;
            case 'ArrowDown':
                this.panBy(0, -step);
                event.preventDefault();
                break;
            case 'ArrowLeft':
                this.panBy(step, 0);
                event.preventDefault();
                break;
            case 'ArrowRight':
                this.panBy(-step, 0);
                event.preventDefault();
                break;
            case '+':
            case '=':
                this.zoomIn();
                event.preventDefault();
                break;
            case '-':
            case '_':
                this.zoomOut();
                event.preventDefault();
                break;
            case '0':
                this.reset();
                event.preventDefault();
                break;
        }
    }
}
