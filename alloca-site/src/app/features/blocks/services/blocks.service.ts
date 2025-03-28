import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { BlockListItem, CreateBlockRequest, CreateBlockResponse } from '@features/blocks/models/block.model';

@Injectable({ providedIn: 'root' })
export class BlocksService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiBaseUrl}/manager`;

    createBlock(req: CreateBlockRequest): Observable<CreateBlockResponse> {
        return this.http.post<CreateBlockResponse>(`${this.base}/blocks`, req);
    }

    listBlocks(opts?: { pavilionId?: string | null; includeExpired?: boolean }): Observable<BlockListItem[]> {
        let params = new HttpParams();
        if (opts?.pavilionId) params = params.set('pavilionId', opts.pavilionId);
        if (opts?.includeExpired) params = params.set('includeExpired', 'true');
        return this.http.get<BlockListItem[]>(`${this.base}/blocks`, { params });
    }
}
