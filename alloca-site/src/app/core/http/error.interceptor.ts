import { HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { MessageService } from 'primeng/api';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(MessageService);
    const injector = inject(Injector);

    return next(req).pipe(
        catchError((err) => {
            const translate = injector.get(TranslateService);
            const status = err?.status ?? 0;
            const code: string | undefined = err?.error?.code ?? err?.error?.errorCode;
            const args: unknown[] = Array.isArray(err?.error?.args) ? err.error.args : [];
            const params: Record<string, unknown> = {};
            args.forEach((v, i) => (params[String(i)] = v));

            const mostrar = (chaveCategoria: string) => {
                const chaveCodigo = code ? `errors.codes.${code}` : null;
                const sumario = chaveCodigo ? translate.instant(chaveCodigo, params) : null;
                const sumarioValido = sumario && sumario !== chaveCodigo;
                toast.add({
                    severity: 'error',
                    summary: translate.instant(`${chaveCategoria}.summary`),
                    detail: sumarioValido ? sumario : err?.error?.detail || err?.error?.message || translate.instant(`${chaveCategoria}.detail`),
                    life: 5000
                });
            };

            if (status === 0) {
                mostrar('errors.network');
            } else if (status >= 500) {
                mostrar('errors.server');
            }

            return throwError(() => err);
        })
    );
};
