import { Routes } from '@angular/router';

export default [
    { path: '', pathMatch: 'full', redirectTo: 'reserve' },
    {
        path: 'reserve',
        title: 'Reservar',
        loadComponent: () => import('@features/reservations/pages/reserve/reserve.page').then((m) => m.ReservePage)
    },
    {
        path: 'reservations',
        title: 'Minhas reservas',
        loadComponent: () => import('@features/reservations/pages/my-reservations/my-reservations.page').then((m) => m.MyReservationsPage)
    },

    { path: 'checkin', pathMatch: 'full', redirectTo: 'reservations' }
] as Routes;
