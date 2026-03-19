import { Routes } from '@angular/router';
import { Notfound } from './app/shared/pages/notfound/notfound';
import { AppLayout } from './app/shared/components/app-layout/app.layout';
import { authGuard } from './app/core/auth/guards/auth.guard';
import { roleGuard } from './app/core/auth/guards/role.guard';
import { redirectByRoleGuard } from './app/core/auth/guards/redirect-by-role.guard';

export const appRoutes: Routes = [
    {
        path: '',
        pathMatch: 'full',
        canMatch: [redirectByRoleGuard],
        children: []
    },
    {
        path: 'auth',
        loadChildren: () => import('./app/features/auth/auth.routes')
    },
    {
        path: 'student',
        component: AppLayout,
        canMatch: [authGuard, roleGuard(['Member', 'Admin'])],
        loadChildren: () => import('./app/routes/student.routes')
    },
    {
        path: 'manager',
        component: AppLayout,
        canMatch: [authGuard, roleGuard(['PavilionManager', 'Admin'])],
        loadChildren: () => import('./app/routes/manager.routes')
    },
    { path: 'notfound', component: Notfound },
    { path: '**', redirectTo: '/notfound' }
];
