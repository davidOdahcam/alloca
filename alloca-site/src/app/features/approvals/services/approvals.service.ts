import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@/environments/environment';
import { ReasonRequest } from '@/app/features/blocks/models/block.model';
import { Reservation } from '@/app/features/reservations/models/reservation.model';

@Injectable({ providedIn: 'root' })
export class ApprovalsService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiBaseUrl}/manager`;

    listPending(pavilionId?: string | null): Observable<Reservation[]> {
        let params = new HttpParams();
        if (pavilionId) params = params.set('pavilionId', pavilionId);
        return this.http.get<Reservation[]>(`${this.base}/reservations/pending`, { params });
    }

    approve(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/reservations/${id}/approve`, {});
    }

    reject(id: string, reason: ReasonRequest): Observable<void> {
        return this.http.post<void>(`${this.base}/reservations/${id}/reject`, reason);
    }

    revoke(id: string, reason: ReasonRequest): Observable<void> {
        return this.http.post<void>(`${this.base}/reservations/${id}/revoke`, reason);
    }
}
