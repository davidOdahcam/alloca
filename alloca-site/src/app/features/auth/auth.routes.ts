import { Routes } from '@angular/router';
import { Access } from './pages/access/access.page';
import { Error } from './pages/error/error.page';
import { Login } from './pages/login/login.page';

export default [
    { path: 'access', component: Access },
    { path: 'error', component: Error },
    { path: 'login', component: Login }
] as Routes;
