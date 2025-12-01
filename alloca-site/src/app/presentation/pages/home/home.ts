import { Component } from '@angular/core';

@Component({
    selector: 'app-home',
    standalone: true,
    template: ` <div class="card">
        <div class="font-semibold text-xl mb-4">Home Page</div>
        <p>Welcome to the home page. Use this page to start from scratch and place your custom content.</p>
    </div>`
})
export class Home {}
