import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-brand-lockup',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './brand-lockup.component.html',
  styleUrl: './brand-lockup.component.scss'
})
export class BrandLockupComponent {
  /** Height and typography scale */
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  /** `dark`: light text on dark surfaces. `light`: navy on pale surfaces */
  @Input() variant: 'dark' | 'light' = 'light';
}
