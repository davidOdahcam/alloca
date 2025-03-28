import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { CreateUserRequest, ResetUserPasswordRequest, UpdateUserRequest, UserDetail, UserListItem, UserRole } from '@features/users/models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
    private readonly http = inject(HttpClient);
    private readonly base = `${environment.apiBaseUrl}/users`;

    list(filtros?: { search?: string | null; role?: UserRole | null; isActive?: boolean | null }): Observable<UserListItem[]> {
        let params = new HttpParams();
        if (filtros?.search) params = params.set('search', filtros.search);
        if (filtros?.role) params = params.set('role', filtros.role);
        if (filtros?.isActive !== null && filtros?.isActive !== undefined) {
            params = params.set('isActive', String(filtros.isActive));
        }
        return this.http.get<UserListItem[]>(this.base, { params });
    }

    get(id: string): Observable<UserDetail> {
        return this.http.get<UserDetail>(`${this.base}/${id}`);
    }

    create(request: CreateUserRequest): Observable<UserDetail> {
        return this.http.post<UserDetail>(this.base, request);
    }

    update(id: string, request: UpdateUserRequest): Observable<UserDetail> {
        return this.http.put<UserDetail>(`${this.base}/${id}`, request);
    }

    deactivate(id: string): Observable<void> {
        return this.http.patch<void>(`${this.base}/${id}/deactivate`, {});
    }

    activate(id: string): Observable<void> {
        return this.http.patch<void>(`${this.base}/${id}/activate`, {});
    }

    resetPassword(id: string, request: ResetUserPasswordRequest): Observable<void> {
        return this.http.patch<void>(`${this.base}/${id}/password`, request);
    }
}
