import { Routes } from '@angular/router';
import { Access } from './pages/access';
import { Error } from './pages/error';
import { Login } from './pages/login';

export default [
    { path: 'access', component: Access },
    { path: 'error', component: Error },
    { path: 'login', component: Login }
] as Routes;
