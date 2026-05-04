import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import type { AuthResponse } from './task.models';

const TOKEN_KEY = 'tm_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly tokenSignal = signal<string | null>(sessionStorage.getItem(TOKEN_KEY));
  readonly token = this.tokenSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.tokenSignal());

  login(body: { email: string; password: string }) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, body).pipe(
      tap((r) => this.persist(r)),
    );
  }

  register(body: { email: string; password: string; displayName: string }) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, body).pipe(
      tap((r) => this.persist(r)),
    );
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    this.tokenSignal.set(null);
    void this.router.navigateByUrl('/login');
  }

  private persist(r: AuthResponse): void {
    sessionStorage.setItem(TOKEN_KEY, r.token);
    this.tokenSignal.set(r.token);
  }
}
