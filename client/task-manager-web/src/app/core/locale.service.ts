import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, Injectable, inject, signal } from '@angular/core';
import type { AbstractControl, ValidationErrors } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

const STORAGE_KEY = 'tm_lang';

function getNested(obj: Record<string, unknown> | undefined, path: string): string | undefined {
  if (!obj) {
    return undefined;
  }
  const parts = path.split('.');
  let cur: unknown = obj;
  for (const p of parts) {
    if (cur === null || typeof cur !== 'object') {
      return undefined;
    }
    cur = (cur as Record<string, unknown>)[p];
  }
  return typeof cur === 'string' ? cur : undefined;
}

export function showControlErrors(control: AbstractControl | null, submitted: boolean): boolean {
  if (!control) {
    return false;
  }
  return control.invalid && (submitted || control.touched || control.dirty);
}

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly http = inject(HttpClient);
  private readonly appRef = inject(ApplicationRef);
  private readonly catalogInternal = signal<Record<string, unknown>>({});

  /** Loaded messages for the current language (read in templates / translate pipe). */
  readonly catalog = this.catalogInternal.asReadonly();

  readonly lang = signal<'en' | 'pt'>('en');

  async initialize(): Promise<void> {
    const saved = localStorage.getItem(STORAGE_KEY) as 'en' | 'pt' | null;
    const fallback: 'en' | 'pt' = navigator.language.toLowerCase().startsWith('pt') ? 'pt' : 'en';
    const next = saved === 'pt' || saved === 'en' ? saved : fallback;
    this.lang.set(next);
    await this.loadCatalog(next);
  }

  async setLanguage(next: 'en' | 'pt'): Promise<void> {
    localStorage.setItem(STORAGE_KEY, next);
    this.lang.set(next);
    await this.loadCatalog(next);
  }

  t(key: string, params?: Record<string, string>): string {
    let text = getNested(this.catalogInternal(), key) ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        text = text.replaceAll(`{{${k}}}`, v);
      }
    }
    return text;
  }

  dateLocale(): string {
    return this.lang() === 'pt' ? 'pt' : 'en-US';
  }

  /** Column format for task due date (day/month/year when UI is Portuguese). */
  dueDateColumnFormat(): string {
    return this.lang() === 'pt' ? 'dd/MM/yyyy, HH:mm' : 'short';
  }

  validationMessages(errors: ValidationErrors | null | undefined): string[] {
    if (!errors) {
      return [];
    }
    const out: string[] = [];
    if (errors['required']) {
      out.push(this.t('validation.required'));
    }
    if (errors['email']) {
      out.push(this.t('validation.email'));
    }
    if (errors['minlength']) {
      const n = String((errors['minlength'] as { requiredLength: number }).requiredLength);
      out.push(this.t('validation.minLength', { n }));
    }
    if (errors['maxlength']) {
      const n = String((errors['maxlength'] as { requiredLength: number }).requiredLength);
      out.push(this.t('validation.maxLength', { n }));
    }
    if (errors['passwordMismatch'] || errors['mismatch']) {
      out.push(this.t('validation.passwordMismatch'));
    }
    return out;
  }

  parseHttpError(err: unknown, fallbackTranslationKey: string): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error;
      if (body && typeof body === 'object' && 'detail' in body) {
        const d = (body as { detail?: string }).detail;
        if (d?.trim()) {
          return d.trim();
        }
      }
      if (typeof body === 'string' && body.trim()) {
        try {
          const o = JSON.parse(body) as { detail?: string; title?: string };
          if (o.detail?.trim()) {
            return o.detail.trim();
          }
          if (o.title?.trim()) {
            return o.title.trim();
          }
        } catch {
          return body;
        }
      }
      if (err.status === 401) {
        return this.t('error.unauthorized');
      }
      if (err.status === 409) {
        return this.t('error.conflict');
      }
      if (err.status === 0) {
        return this.t('error.network');
      }
    }
    return this.t(fallbackTranslationKey);
  }

  private async loadCatalog(next: 'en' | 'pt'): Promise<void> {
    const data = await firstValueFrom(this.http.get<Record<string, unknown>>(`/i18n/${next}.json`));
    this.catalogInternal.set(data);
    // Ensure views refresh after async JSON load (some setups coalesce CD; pipes may not repaint until interaction).
    queueMicrotask(() => {
      try {
        this.appRef.tick();
      } catch {
        /* e.g. tests or tick before bootstrap complete */
      }
    });
  }
}
