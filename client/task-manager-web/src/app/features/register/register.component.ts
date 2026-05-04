import { Component, DestroyRef, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth.service';
import { LocaleService, showControlErrors } from '../../core/locale.service';
import { TranslatePipe } from '../../core/translate.pipe';
import { matchField } from '../../shared/password-match.validator';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly locale = inject(LocaleService);

  protected readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required, matchField('password')]],
  });

  protected submitted = false;
  protected apiError = '';
  protected busy = false;

  constructor() {
    this.form.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.form.controls.confirmPassword.updateValueAndValidity({ emitEvent: false });
    });
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.apiError = '';
    });
  }

  protected showField(name: 'displayName' | 'email' | 'password' | 'confirmPassword'): boolean {
    return showControlErrors(this.form.get(name), this.submitted);
  }

  protected fieldMsgs(name: 'displayName' | 'email' | 'password' | 'confirmPassword'): string[] {
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

    const { displayName, email, password } = this.form.getRawValue();
    this.busy = true;
    this.auth.register({ displayName, email, password }).subscribe({
      next: () => void this.router.navigateByUrl('/tasks'),
      error: (err) => {
        this.busy = false;
        this.apiError = this.locale.parseHttpError(err, 'register.errorRegister');
      },
      complete: () => {
        this.busy = false;
      },
    });
  }
}
