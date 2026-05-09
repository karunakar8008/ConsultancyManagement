import { DatePipe } from '@angular/common';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { map } from 'rxjs/operators';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../core/services/auth.service';
import { AppNotification, NotificationsService } from '../../core/services/notifications.service';
import { BrandLockupComponent } from '../brand-lockup/brand-lockup.component';
import { ClientLogoMarkComponent } from '../client-logo-mark/client-logo-mark.component';
import { AdminInterviewCalendarPaneComponent } from '../../admin/admin-interview-calendar-pane.component';
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
    MatMenuModule,
    MatBadgeModule,
    BrandLockupComponent,
    ClientLogoMarkComponent,
    AdminInterviewCalendarPaneComponent,
    DatePipe
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpoint = inject(BreakpointObserver);

  /** Match Angular Material / typical tablet breakpoint: drawer overlays below this width. */
  private readonly layoutCompactQuery = '(max-width: 959px)';

  readonly clientDisplayName = environment.clientDisplayName;

  /** Overlay sidenav + hamburger when true (phones / small tablets portrait). */
  isCompactLayout = false;

  /** When false in compact mode the drawer is hidden; on desktop the drawer stays open. */
  sidenavOpened = true;

  unreadCount = 0;
  notificationItems: AppNotification[] = [];

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

  ngOnInit(): void {
    const initiallyCompact = this.breakpoint.isMatched(this.layoutCompactQuery);
    this.isCompactLayout = initiallyCompact;
    this.sidenavOpened = !initiallyCompact;

    this.refreshUnread();
    interval(45_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshUnread());

    this.breakpoint
      .observe(this.layoutCompactQuery)
      .pipe(
        map((r) => r.matches),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((compact) => {
        this.isCompactLayout = compact;
        this.sidenavOpened = !compact;
      });
  }

  toggleSidenav(): void {
    this.sidenavOpened = !this.sidenavOpened;
  }

  closeSidenavAfterNav(): void {
    if (this.isCompactLayout) {
      this.sidenavOpened = false;
    }
  }

  refreshUnread(): void {
    this.notifications.unreadCount().subscribe({
      next: (r) => (this.unreadCount = r.count),
      error: () => {}
    });
  }

  loadNotifications(): void {
    this.notifications.list().subscribe({
      next: (items) => (this.notificationItems = items),
      error: () => {}
    });
  }

  routeForNotification(n: AppNotification): string | null {
    const roles = this.auth.getRoles();
    switch (n.kind) {
      case 'DocumentFromManagement':
      case 'DocumentReviewed':
        if (roles.includes('Consultant')) return '/consultant/documents';
        break;
      case 'DocumentPendingReview':
        if (roles.includes('Admin') || roles.includes('Management')) return '/management/documents';
        if (roles.includes('SalesRecruiter')) return '/sales/assigned-consultants';
        break;
      case 'OnboardingTaskAssigned':
        if (roles.includes('Consultant')) return '/consultant/dashboard';
        break;
      default:
        break;
    }
    return null;
  }

  onNotificationClick(n: AppNotification): void {
    const dest = this.routeForNotification(n);
    this.notifications.markRead(n.id).subscribe({
      next: () => {
        this.refreshUnread();
        if (dest) void this.router.navigateByUrl(dest);
      },
      error: () => {}
    });
  }

  markAllRead(): void {
    this.notifications.markAllRead().subscribe({
      next: () => {
        this.refreshUnread();
        this.loadNotifications();
      },
      error: () => {}
    });
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

  /** Right-side interview calendar for Admin on wider layouts only. */
  showAdminCalendar(): boolean {
    return this.auth.hasRole('Admin') && !this.isCompactLayout;
  }
}
