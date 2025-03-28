import type { UserRole } from '@core/auth/models/auth.model';

export interface UserListItem {
    id: string;
    fullName: string;
    email: string;
    role: UserRole;
    isActive: boolean;
    createdAt: string;
}

export interface UserDetail extends UserListItem {}

export interface CreateUserRequest {
    fullName: string;
    email: string;
    password: string;
    role: UserRole;
}

export interface UpdateUserRequest {
    fullName: string;
    role: UserRole;
}

export interface ResetUserPasswordRequest {
    newPassword: string;
}
