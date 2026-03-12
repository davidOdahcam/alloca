import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@/app/core/auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const token = auth.token();
    const authedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

    return next(authedReq).pipe(
        catchError((err) => {
            if (err.status === 401) {
                auth.logout();
                router.navigate(['/auth/login']);
            } else if (err.status === 403) {
                router.navigate(['/auth/access']);
            }
            return throwError(() => err);
        })
    );
};
