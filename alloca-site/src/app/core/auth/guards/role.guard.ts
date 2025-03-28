import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { UserRole } from '@core/auth/models/auth.model';

export const roleGuard = (allowed: UserRole[]): CanMatchFn => {
    return () => {
        const auth = inject(AuthService);
        const router = inject(Router);

        if (!auth.isAuthenticated()) return router.createUrlTree(['/auth/login']);

        const role = auth.role();
        if (role && allowed.includes(role)) return true;

        return router.createUrlTree(['/auth/access']);
    };
};
