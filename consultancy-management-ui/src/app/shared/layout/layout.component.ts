import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/services/auth.service';
import { BrandLockupComponent } from '../brand-lockup/brand-lockup.component';
import { ClientLogoMarkComponent } from '../client-logo-mark/client-logo-mark.component';
import { environment } from '../../../environments/environment';

export interface NavItem {
  label: string;
  path: string;
  icon: string;
  roles: string[];
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatToolbarModule,
    MatButtonModule,
    BrandLockupComponent,
    ClientLogoMarkComponent
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  private readonly auth = inject(AuthService);

  readonly clientDisplayName = environment.clientDisplayName;

  readonly allNav: NavItem[] = [
    { label: 'Dashboard', path: '/admin/dashboard', icon: 'dashboard', roles: ['Admin'] },
    { label: 'Users', path: '/admin/users', icon: 'people', roles: ['Admin'] },
    { label: 'Consultants', path: '/admin/consultants', icon: 'engineering', roles: ['Admin'] },
    { label: 'Sales Recruiters', path: '/admin/sales-recruiters', icon: 'headset_mic', roles: ['Admin'] },
    { label: 'Management', path: '/admin/management-users', icon: 'business', roles: ['Admin'] },
    { label: 'Assignments', path: '/admin/assignments', icon: 'link', roles: ['Admin'] },
    { label: 'Reports', path: '/admin/reports', icon: 'bar_chart', roles: ['Admin'] },
    {
      label: 'All files & proofs',
      path: '/management/documents',
      icon: 'folder_special',
      roles: ['Admin']
    },

    { label: 'Dashboard', path: '/consultant/dashboard', icon: 'dashboard', roles: ['Consultant'] },
    { label: 'Daily Activities', path: '/consultant/daily-activities', icon: 'today', roles: ['Consultant'] },
    { label: 'Job Applications', path: '/consultant/job-applications', icon: 'work', roles: ['Consultant'] },
    { label: 'Vendors reached', path: '/consultant/vendor-reach-outs', icon: 'storefront', roles: ['Consultant'] },
    { label: 'Submissions', path: '/consultant/submissions', icon: 'send', roles: ['Consultant'] },
    { label: 'Interviews', path: '/consultant/interviews', icon: 'event', roles: ['Consultant'] },
    { label: 'Documents', path: '/consultant/documents', icon: 'folder', roles: ['Consultant'] },
    { label: 'Profile', path: '/consultant/profile', icon: 'person', roles: ['Consultant'] },

    { label: 'Dashboard', path: '/sales/dashboard', icon: 'dashboard', roles: ['SalesRecruiter'] },
    { label: 'Assigned Consultants', path: '/sales/assigned-consultants', icon: 'groups', roles: ['SalesRecruiter'] },
    { label: 'Vendors', path: '/sales/vendors', icon: 'store', roles: ['SalesRecruiter'] },
    { label: 'Submissions', path: '/sales/submissions', icon: 'assignment', roles: ['SalesRecruiter'] },
    { label: 'Interviews', path: '/sales/interviews', icon: 'event', roles: ['SalesRecruiter'] },

    { label: 'Dashboard', path: '/management/dashboard', icon: 'dashboard', roles: ['Management'] },
    { label: 'Onboarding', path: '/management/onboarding', icon: 'checklist', roles: ['Management'] },
    { label: 'Documents', path: '/management/documents', icon: 'description', roles: ['Management'] },
    { label: 'Submissions', path: '/management/submissions', icon: 'assignment', roles: ['Management'] },
    { label: 'Interviews', path: '/management/interviews', icon: 'event', roles: ['Management'] },
    { label: 'Reports', path: '/management/reports', icon: 'insights', roles: ['Management'] }
  ];

  get navItems(): NavItem[] {
    const roles = this.auth.getRoles();
    return this.allNav.filter((n) => n.roles.some((r) => roles.includes(r)));
  }

  logout(): void {
    this.auth.logout();
  }

  userName(): string {
    return this.auth.getCurrentUser()?.fullName ?? '';
  }

  employeeId(): string {
    return this.auth.getCurrentUser()?.employeeId ?? '';
  }
}
