import { HttpInterceptorFn } from '@angular/common/http';
import { lerIdiomaAtual } from '@core/i18n/language.service';

export const languageInterceptor: HttpInterceptorFn = (req, next) => {
    const cloned = req.clone({ setHeaders: { 'Accept-Language': lerIdiomaAtual() } });
    return next(cloned);
};
