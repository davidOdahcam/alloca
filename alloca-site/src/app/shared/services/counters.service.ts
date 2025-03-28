import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '@core/auth/auth.service';
import { ApprovalsService } from '@features/approvals/services/approvals.service';
import { ReservationService } from '@features/reservations/services/reservation.service';
import { catchError, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CountersService {
    private readonly auth = inject(AuthService);
    private readonly manager = inject(ApprovalsService);
    private readonly reservations = inject(ReservationService);

    private readonly _aprovacoesPendentes = signal(0);
    private readonly _minhasProximas = signal(0);

    readonly aprovacoesPendentes = computed(() => this._aprovacoesPendentes());
    readonly minhasProximas = computed(() => this._minhasProximas());

    refresh(): void {
        const role = this.auth.role();
        if (!role) return;

        if (role === 'PavilionManager' || role === 'Admin') {
            this.manager
                .listPending(null)
                .pipe(catchError(() => of([])))
                .subscribe((list) => this._aprovacoesPendentes.set(list.length));
        }

        if (role === 'Member' || role === 'Admin') {
            this.reservations
                .listMine()
                .pipe(catchError(() => of([])))
                .subscribe((list) => {
                    const agora = Date.now();
                    const proximas = list.filter((r) => {
                        const inicio = new Date(r.startUtc).getTime();
                        return (r.status === 'Pending' || r.status === 'Approved' || r.status === 'InProgress') && inicio + 4 * 60 * 60 * 1000 >= agora;
                    }).length;
                    this._minhasProximas.set(proximas);
                });
        }
    }

    aprovacoesDecremento(): void {
        this._aprovacoesPendentes.update((v) => Math.max(0, v - 1));
    }

    minhasIncremento(): void {
        this._minhasProximas.update((v) => v + 1);
    }
}
