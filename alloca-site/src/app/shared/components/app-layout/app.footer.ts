import { Component, signal } from '@angular/core';

@Component({
    standalone: true,
    selector: 'app-footer',
    template: `<div class="layout-footer">&copy; {{ currentYear() }} Alloca. Todos os direitos reservados.</div>`
})
export class AppFooter {
    readonly currentYear = signal(new Date().getFullYear()).asReadonly();
}
