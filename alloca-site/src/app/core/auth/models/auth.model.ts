export type UserRole = 'Member' | 'PavilionManager' | 'Admin';

export interface AuthUser {
    id: string;
    fullName: string;
    email: string;
    role: UserRole;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    expiresAtUtc: string;
    userId: string;
    fullName: string;
    email: string;
    role: UserRole;
}
