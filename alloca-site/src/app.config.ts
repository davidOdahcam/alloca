import { DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeuix/themes/aura';
import { providePrimeNG, PrimeNG } from 'primeng/config';
import { MessageService, ConfirmationService } from 'primeng/api';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { firstValueFrom } from 'rxjs';
import { appRoutes } from './app.routes';
import { authInterceptor } from './app/core/auth/interceptors/auth.interceptor';
import { errorInterceptor } from './app/core/http/error.interceptor';
import { languageInterceptor } from './app/core/http/language.interceptor';
import { lerIdiomaAtual } from './app/core/i18n/language.service';

// Fuso horário de Brasília. As datas são armazenadas em UTC no banco e devem ser
// sempre exibidas no horário de Brasília, independentemente do fuso do navegador.
// O DatePipe do Angular não aceita nomes IANA (America/Sao_Paulo), apenas offsets.
// O Brasil não observa horário de verão desde 2019, então o offset fixo -0300 é estável.
const FUSO_BRASILIA = '-0300';

export const appConfig: ApplicationConfig = {
    providers: [
        { provide: DATE_PIPE_DEFAULT_OPTIONS, useValue: { timezone: FUSO_BRASILIA } },
        provideRouter(appRoutes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([languageInterceptor, authInterceptor, errorInterceptor])),
        provideZonelessChangeDetection(),
        provideTranslateService({
            fallbackLang: 'pt-BR',
            lang: lerIdiomaAtual(),
            loader: provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' })
        }),
        providePrimeNG({
            theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } }
        }),
        MessageService,
        ConfirmationService,

        provideAppInitializer(() => {
            const translate = inject(TranslateService);
            const primeng = inject(PrimeNG);
            const idioma = lerIdiomaAtual();
            return firstValueFrom(translate.use(idioma)).then(() => {
                const traducaoPrimeng = translate.instant('primeng');
                if (traducaoPrimeng && typeof traducaoPrimeng === 'object') {
                    primeng.setTranslation(traducaoPrimeng);
                }
                document.documentElement.lang = idioma;
            });
        })
    ]
};
