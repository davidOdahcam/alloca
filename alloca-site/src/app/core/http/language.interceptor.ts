import { HttpInterceptorFn } from '@angular/common/http';
import { lerIdiomaAtual } from '@core/i18n/language.service';

/**
 * Adiciona o cabeçalho Accept-Language em todas as requisições HTTP.
 *
 * Importante: lê o idioma diretamente de `localStorage` (via `lerIdiomaAtual`)
 * em vez de injetar `LanguageService`, evitando ciclo de DI
 * (LanguageService → TranslateService → HttpClient → languageInterceptor).
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
    const cloned = req.clone({ setHeaders: { 'Accept-Language': lerIdiomaAtual() } });
    return next(cloned);
};
