import { Routes } from '@angular/router';
import { Notfound } from './app/presentation/pages/notfound/notfound';
import { AppLayout } from './app/presentation/components/app.layout';
import { Home } from './app/presentation/pages/home/home';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        children: [{ path: '', component: Home }]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/features/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
