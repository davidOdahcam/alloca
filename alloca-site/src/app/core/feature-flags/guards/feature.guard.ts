import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { environment } from '@env/environment';

type FeatureKey = keyof typeof environment.features;

/**
 * Bloqueia o acesso à rota se a feature flag estiver desligada.
 * Retorna redirect para a home conforme o papel do usuário.
 */
export const featureGuard = (feature: FeatureKey): CanMatchFn => {
    return () => {
        const router = inject(Router);
        if (environment.features[feature]) return true;
        return router.createUrlTree(['/']);
    };
};
