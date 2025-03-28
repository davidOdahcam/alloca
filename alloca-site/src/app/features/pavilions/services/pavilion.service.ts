import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '@env/environment';
import { AvailabilityResource, CheckAvailabilityRequest, Floor, FloorResources, Pavilion } from '@features/pavilions/models/pavilion.model';

@Injectable({ providedIn: 'root' })
export class PavilionService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiBaseUrl}/pavilions`;

    private cachePavilions?: Observable<Pavilion[]>;
    private readonly cacheFloorsPorPavilion = new Map<string, Observable<Floor[]>>();

    list(): Observable<Pavilion[]> {
        if (!this.cachePavilions) {
            this.cachePavilions = this.http.get<Pavilion[]>(this.base).pipe(shareReplay({ bufferSize: 1, refCount: false }));
        }
        return this.cachePavilions;
    }

    listFloors(pavilionId: string): Observable<Floor[]> {
        let cache = this.cacheFloorsPorPavilion.get(pavilionId);
        if (!cache) {
            cache = this.http.get<Floor[]>(`${this.base}/${pavilionId}/floors`).pipe(shareReplay({ bufferSize: 1, refCount: false }));
            this.cacheFloorsPorPavilion.set(pavilionId, cache);
        }
        return cache;
    }

    /** Limpa caches (útil após login/logout ou alterações administrativas). */
    invalidarCache(): void {
        this.cachePavilions = undefined;
        this.cacheFloorsPorPavilion.clear();
    }

    checkAvailability(pavilionId: string, floorId: string, request: CheckAvailabilityRequest): Observable<AvailabilityResource[]> {
        return this.http.post<AvailabilityResource[]>(`${this.base}/${pavilionId}/floors/${floorId}/availability`, request);
    }

    listResources(pavilionId: string, floorId: string): Observable<FloorResources> {
        return this.http.get<FloorResources>(`${this.base}/${pavilionId}/floors/${floorId}/resources`);
    }
}
