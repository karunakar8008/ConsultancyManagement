import { Component } from '@angular/core';
import { BrandLockupComponent } from '../brand-lockup/brand-lockup.component';
import { ClientLogoMarkComponent } from '../client-logo-mark/client-logo-mark.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-brand-banner',
  standalone: true,
  imports: [BrandLockupComponent, ClientLogoMarkComponent],
  templateUrl: './brand-banner.component.html',
  styleUrl: './brand-banner.component.scss'
})
export class BrandBannerComponent {
  readonly clientDisplayName = environment.clientDisplayName;
}
