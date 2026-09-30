import { Routes } from '@angular/router';
import { roleGuard } from '@core/auth/guards/role.guard';

export default [
    { path: '', pathMatch: 'full', loadComponent: () => import('@features/approvals/pages/approvals/approvals.page').then((m) => m.ApprovalsPage), title: 'Pedidos' },
    {
        path: 'approvals',
        title: 'Pedidos',
        loadComponent: () => import('@features/approvals/pages/approvals/approvals.page').then((m) => m.ApprovalsPage)
    },
    {
        path: 'blocks',
        title: 'Bloqueios',
        loadComponent: () => import('@features/blocks/pages/blocks/blocks.page').then((m) => m.BlocksPage)
    },
    {
        path: 'blocks/novo',
        title: 'Novo bloqueio',
        loadComponent: () => import('@features/blocks/pages/block-create/block-create.page').then((m) => m.BlockCreatePage)
    },
    {
        path: 'users',
        title: 'Usuários',
        canMatch: [roleGuard(['Admin'])],
        loadComponent: () => import('@features/users/pages/users/users.page').then((m) => m.UsersPage)
    }
] as Routes;
