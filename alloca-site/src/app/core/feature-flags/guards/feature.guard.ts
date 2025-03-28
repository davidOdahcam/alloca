import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { environment } from '@env/environment';

type FeatureKey = keyof typeof environment.features;

export const featureGuard = (feature: FeatureKey): CanMatchFn => {
    return () => {
        const router = inject(Router);
        if (environment.features[feature]) return true;
        return router.createUrlTree(['/']);
    };
};
