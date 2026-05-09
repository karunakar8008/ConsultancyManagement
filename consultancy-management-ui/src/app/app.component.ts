import { DOCUMENT } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterOutlet } from '@angular/router';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class AppComponent {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly doc = inject<Document>(DOCUMENT);

  constructor() {
    this.applyClientBranding();
  }

  private applyClientBranding(): void {
    const client = environment.clientDisplayName;
    /** Browser tab (compact); defaults to full client name if unset */
    const pageTitle = environment.clientTabTitle ?? client;

    this.title.setTitle(pageTitle);

    const description = `${client} — official portal for consultant onboarding, documents, submissions, interviews, and reporting.`;

    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ name: 'application-name', content: client });
    this.meta.updateTag({ name: 'apple-mobile-web-app-title', content: pageTitle });

    this.meta.updateTag({ property: 'og:title', content: client });
    this.meta.updateTag({ property: 'og:site_name', content: client });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:type', content: 'website' });

    this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
    this.meta.updateTag({ name: 'twitter:title', content: client });
    this.meta.updateTag({ name: 'twitter:description', content: description });

    const origin =
      typeof globalThis !== 'undefined' && 'location' in globalThis
        ? (globalThis as unknown as { location?: { origin?: string } }).location?.origin ?? ''
        : '';
    const iconPath = '/assets/branding/client-favicon.svg';
    const logoAbs = origin ? `${origin}${iconPath}` : iconPath;
    if (origin) {
      this.meta.updateTag({ property: 'og:image', content: logoAbs });
      this.meta.updateTag({ property: 'og:image:alt', content: `${client} — logo` });
      this.meta.updateTag({ property: 'og:image:type', content: 'image/svg+xml' });
      this.meta.updateTag({ name: 'twitter:image', content: logoAbs });
      this.meta.updateTag({ name: 'twitter:image:alt', content: `${client} — logo` });
    }

    this.injectOrganizationJsonLd(client, origin, logoAbs);

    this.meta.updateTag({ name: 'theme-color', content: '#1e3a8a' });

    let link = this.doc.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'icon');
      this.doc.head.appendChild(link);
    }
    link.type = 'image/svg+xml';
    link.href = logoAbs;
  }

  /** Helps search engines associate the tab title (client name) with the client logo */
  private injectOrganizationJsonLd(client: string, origin: string, logoUrl: string): void {
    const existing = this.doc.getElementById('tenant-org-jsonld');
    existing?.remove();

    const payload: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      name: client,
      logo: logoUrl
    };
    if (origin) {
      payload['url'] = origin;
    }

    const script = this.doc.createElement('script');
    script.id = 'tenant-org-jsonld';
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(payload);
    this.doc.head.appendChild(script);
  }
}
