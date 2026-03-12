import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '@/app/core/auth/auth.service';

export const redirectByRoleGuard: CanMatchFn = () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    return router.parseUrl(auth.isAuthenticated() ? auth.homePathForRole() : '/auth/login');
};
