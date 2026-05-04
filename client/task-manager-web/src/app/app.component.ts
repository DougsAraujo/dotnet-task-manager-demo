import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { LocaleService } from './core/locale.service';
import { TranslatePipe } from './core/translate.pipe';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, TranslatePipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
  protected readonly locale = inject(LocaleService);

  protected async setLang(next: 'en' | 'pt'): Promise<void> {
    await this.locale.setLanguage(next);
  }
}
