import { HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { MessageService } from 'primeng/api';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';

/**
 * Mapeia erros HTTP em mensagens amigáveis usando chaves i18n.
 *
 * Estratégia de tradução (prioridade):
 *   1. Se o backend devolver `error.error.code`, busca em `errors.codes.<code>`
 *      passando `error.error.args` como contexto (interpolação `{{0}}`, `{{1}}`...).
 *   2. Se faltar a chave, cai para a categoria por status HTTP (`errors.network`, `errors.server`, etc.).
 *   3. Como último recurso, usa `errors.unknown`.
 *
 * 401 e 403 são tratados pelo authInterceptor (redireciona), aqui só logamos.
 * 4xx (exceto 401/403) são silenciados aqui — espera-se que as páginas mostrem feedback contextual.
 */
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
            // 4xx (incl. 401/403) seguem o fluxo para os chamadores/authInterceptor.

            return throwError(() => err);
        })
    );
};
