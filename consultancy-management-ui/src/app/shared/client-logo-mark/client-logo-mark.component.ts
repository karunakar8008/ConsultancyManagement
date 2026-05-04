import { Component, Input } from '@angular/core';

/** Placeholder client mark for the top bar; replace with an `<img>` to `assets/client-logo.svg` when you have a real logo file */
@Component({
  selector: 'app-client-logo-mark',
  standalone: true,
  template: `
    <div class="mark-wrap" role="img" [attr.aria-label]="label">
      <svg class="mark-svg" viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <defs>
          <linearGradient [attr.id]="gid" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stop-color="#172554" />
            <stop offset="45%" stop-color="#1d4ed8" />
            <stop offset="100%" stop-color="#38bdf8" />
          </linearGradient>
        </defs>
        <rect x="3" y="3" width="42" height="42" rx="13" [attr.fill]="'url(#' + gid + ')'" />
        <rect x="13" y="22" width="5.5" height="14" rx="2.5" fill="white" opacity="0.92" />
        <rect x="21.25" y="16" width="5.5" height="20" rx="2.5" fill="white" opacity="0.96" />
        <rect x="29.5" y="10" width="5.5" height="26" rx="2.5" fill="white" />
        <circle cx="36" cy="12" r="3" fill="#fef08a" opacity="0.95" />
      </svg>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        line-height: 0;
      }

      .mark-wrap {
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .mark-svg {
        width: var(--client-logo-size, 44px);
        height: var(--client-logo-size, 44px);
        display: block;
        filter: drop-shadow(0 2px 6px rgba(15, 23, 42, 0.18)) drop-shadow(0 1px 2px rgba(30, 64, 175, 0.12));
      }
    `
  ]
})
export class ClientLogoMarkComponent {
  private static _seq = 0;
  readonly gid = `client-logo-grad-${ClientLogoMarkComponent._seq++}`;

  @Input() label = 'Client organization';
}
