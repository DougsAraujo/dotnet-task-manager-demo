import { Component, DestroyRef, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth.service';
import { LocaleService, showControlErrors } from '../../core/locale.service';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly locale = inject(LocaleService);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submitted = false;
  protected apiError = '';
  protected busy = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.apiError = '';
    });
  }

  protected showField(name: 'email' | 'password'): boolean {
    return showControlErrors(this.form.get(name), this.submitted);
  }

  protected fieldMsgs(name: 'email' | 'password'): string[] {
    return this.locale.validationMessages(this.form.get(name)?.errors);
  }

  protected async setLanguage(next: 'en' | 'pt'): Promise<void> {
    await this.locale.setLanguage(next);
  }

  submit(): void {
    this.submitted = true;
    this.apiError = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy = true;
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl('/tasks'),
      error: (err) => {
        this.busy = false;
        this.apiError = this.locale.parseHttpError(err, 'login.errorSignIn');
      },
      complete: () => {
        this.busy = false;
      },
    });
  }
}
