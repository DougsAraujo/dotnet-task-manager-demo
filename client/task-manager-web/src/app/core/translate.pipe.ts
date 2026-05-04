import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from './locale.service';

@Pipe({
  /** Named `translate` (not `t`) so it never collides with `@for` / template variables like `t` or `task`. */
  name: 'translate',
  standalone: true,
  pure: false,
})
export class TranslatePipe implements PipeTransform {
  private readonly locale = inject(LocaleService);

  transform(key: string, params?: Record<string, string> | null): string {
    this.locale.lang();
    this.locale.catalog();
    return this.locale.t(key, params ?? undefined);
  }
}
