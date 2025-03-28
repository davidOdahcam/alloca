import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { CheckInRequest, CreateReservationRequest, CreateReservationResponse, Reservation } from '@features/reservations/models/reservation.model';

@Injectable({ providedIn: 'root' })
export class ReservationService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiBaseUrl}/reservations`;

    listMine(): Observable<Reservation[]> {
        return this.http.get<Reservation[]>(`${this.base}/mine`);
    }

    create(req: CreateReservationRequest): Observable<CreateReservationResponse> {
        return this.http.post<CreateReservationResponse>(this.base, req);
    }

    cancel(id: string): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/cancel`, {});
    }

    checkIn(id: string, req: CheckInRequest): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/checkin`, req);
    }

    qrCode(id: string): Observable<Blob> {
        return this.http.get(`${this.base}/${id}/qrcode`, { responseType: 'blob' });
    }
}
