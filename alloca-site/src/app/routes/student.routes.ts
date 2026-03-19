import { Routes } from '@angular/router';

export default [
    { path: '', pathMatch: 'full', redirectTo: 'reserve' },
    {
        path: 'reserve',
        title: 'Reservar',
        loadComponent: () => import('@/app/features/reservations/pages/reserve/reserve.page').then((m) => m.ReservePage)
    },
    {
        path: 'reservations',
        title: 'Minhas reservas',
        loadComponent: () => import('@/app/features/reservations/pages/my-reservations/my-reservations.page').then((m) => m.MyReservationsPage)
    },
    // Redirecionamento legado do antigo menu de check-in (validação agora é feita por gestor)
    { path: 'checkin', pathMatch: 'full', redirectTo: 'reservations' }
] as Routes;
