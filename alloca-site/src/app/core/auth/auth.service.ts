import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { AuthUser, LoginRequest, LoginResponse, UserRole } from '@core/auth/models/auth.model';
import { StorageKeys, StorageKeysLegacy } from '@core/layout/storage-keys';

const STORAGE_KEY = StorageKeys.auth;

interface PersistedAuth {
    token: string;
    expiresAtUtc: string;
    user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly base = environment.apiBaseUrl;

    private readonly state = signal<PersistedAuth | null>(this.restore());

    readonly token = computed(() => this.state()?.token ?? null);
    readonly currentUser = computed<AuthUser | null>(() => this.state()?.user ?? null);
    readonly isAuthenticated = computed(() => !!this.state() && !this.isExpired());
    readonly role = computed<UserRole | null>(() => this.state()?.user.role ?? null);
    readonly isAdmin = computed(() => this.role() === 'Admin');
    readonly isManager = computed(() => this.role() === 'PavilionManager' || this.role() === 'Admin');
    readonly isStudent = computed(() => this.role() === 'Member' || this.role() === 'Admin');

    login(request: LoginRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.base}/auth/login`, request).pipe(
            tap((res) => {
                const persisted: PersistedAuth = {
                    token: res.token,
                    expiresAtUtc: res.expiresAtUtc,
                    user: { id: res.userId, fullName: res.fullName, email: res.email, role: res.role }
                };
                localStorage.setItem(STORAGE_KEY, JSON.stringify(persisted));
                this.state.set(persisted);
            })
        );
    }

    logout(): void {
        localStorage.removeItem(STORAGE_KEY);
        this.state.set(null);
    }

    homePathForRole(): string {
        const r = this.role();
        if (r === 'PavilionManager') return '/manager';
        if (r === 'Admin') return '/manager';
        if (r === 'Member') return '/student/reserve';
        return '/auth/login';
    }

    private isExpired(): boolean {
        const s = this.state();
        if (!s) return true;
        return new Date(s.expiresAtUtc).getTime() <= Date.now();
    }

    private restore(): PersistedAuth | null {
        try {
            let raw = localStorage.getItem(STORAGE_KEY);

            if (!raw) {
                const legado = localStorage.getItem(StorageKeysLegacy.auth);
                if (legado) {
                    localStorage.setItem(STORAGE_KEY, legado);
                    localStorage.removeItem(StorageKeysLegacy.auth);
                    raw = legado;
                }
            }
            if (!raw) return null;
            const parsed = JSON.parse(raw) as PersistedAuth;
            if (new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
                localStorage.removeItem(STORAGE_KEY);
                return null;
            }
            return parsed;
        } catch {
            return null;
        }
    }
}
