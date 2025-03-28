import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { RippleModule } from 'primeng/ripple';
import { AuthService } from '@core/auth/auth.service';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [ButtonModule, InputTextModule, PasswordModule, FormsModule, RouterModule, RippleModule, MessageModule, TranslatePipe],
    template: `
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, var(--primary-color) 10%, rgba(33, 150, 243, 0) 30%)">
                    <div class="w-full bg-surface-0 dark:bg-surface-900 py-20 px-8 sm:px-20" style="border-radius: 53px">
                        <div class="text-center mb-8">
                            <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-4">{{ 'auth.login.title' | translate }}</div>
                            <span class="text-muted-color font-medium">{{ 'auth.login.subtitle' | translate }}</span>
                        </div>

                        <form (ngSubmit)="submit()" #loginForm="ngForm" novalidate>
                            <label for="email" class="block text-surface-900 dark:text-surface-0 text-xl font-medium mb-2">{{ 'auth.login.emailLabel' | translate }}</label>
                            <input pInputText id="email" name="email" type="email" [placeholder]="'auth.login.emailPlaceholder' | translate" class="w-full md:w-120 mb-8" [(ngModel)]="email" required autocomplete="email" />

                            <label for="password" class="block text-surface-900 dark:text-surface-0 font-medium text-xl mb-2">{{ 'auth.login.passwordLabel' | translate }}</label>
                            <p-password id="password" name="password" [(ngModel)]="password" [placeholder]="'auth.login.passwordPlaceholder' | translate" [toggleMask]="true" styleClass="mb-4" [fluid]="true" [feedback]="false" required />

                            @if (errorMessage()) {
                                <p-message severity="error" [text]="errorMessage()!" styleClass="w-full mb-4" />
                            }

                            <p-button [label]="(loading() ? 'auth.login.submitting' : 'auth.login.submit') | translate" type="submit" styleClass="w-full mt-4" [loading]="loading()" [disabled]="loading() || !email || !password" />
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class Login {
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly translate = inject(TranslateService);

    email = '';
    password = '';
    readonly loading = signal(false);
    readonly errorMessage = signal<string | null>(null);

    submit(): void {
        if (!this.email || !this.password) {
            this.errorMessage.set(this.translate.instant('auth.messages.fillCredentials'));
            return;
        }
        this.loading.set(true);
        this.errorMessage.set(null);
        this.auth.login({ email: this.email, password: this.password }).subscribe({
            next: () => {
                this.loading.set(false);
                this.router.navigateByUrl(this.auth.homePathForRole());
            },
            error: (err) => {
                this.loading.set(false);
                const code: string | undefined = err?.error?.code ?? err?.error?.errorCode;
                let msg: string | null = null;
                if (code) {
                    const traduzido = this.translate.instant(`errors.codes.${code}`);
                    if (traduzido && traduzido !== `errors.codes.${code}`) msg = traduzido;
                }
                if (!msg) msg = err?.error?.detail || err?.error?.message || this.translate.instant('auth.messages.invalidCredentials');
                this.errorMessage.set(msg);
            }
        });
    }
}
