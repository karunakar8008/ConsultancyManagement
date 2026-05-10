import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { LayoutComponent } from './shared/layout/layout.component';
import { LoginComponent } from './auth/login/login.component';
import { RoleRedirectComponent } from './core/redirect/role-redirect.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./auth/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent)
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', component: RoleRedirectComponent },
      {
        path: 'platform/tenants',
        canActivate: [roleGuard],
        data: { roles: ['PlatformAdmin'] },
        loadComponent: () =>
          import('./platform/platform-tenants.component').then((m) => m.PlatformTenantsComponent)
      },
      {
        path: 'admin/dashboard',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./admin/dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent)
      },
      {
        path: 'admin/users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./admin/users/admin-users.component').then((m) => m.AdminUsersComponent)
      },
      {
        path: 'admin/consultants',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./admin/consultants/admin-consultants.component').then((m) => m.AdminConsultantsComponent)
      },
      {
        path: 'admin/sales-recruiters',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./admin/sales-recruiters/admin-sales-recruiters.component').then((m) => m.AdminSalesRecruitersComponent)
      },
      {
        path: 'admin/management-users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./admin/management-users/admin-management-users.component').then(
            (m) => m.AdminManagementUsersComponent
          )
      },
      {
        path: 'admin/assignments',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./admin/assignments/admin-assignments.component').then((m) => m.AdminAssignmentsComponent)
      },
      {
        path: 'admin/reports',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () => import('./admin/reports/admin-reports.component').then((m) => m.AdminReportsComponent)
      },

      {
        path: 'consultant/dashboard',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/dashboard/consultant-dashboard.component').then((m) => m.ConsultantDashboardComponent)
      },
      {
        path: 'consultant/daily-activities',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/daily-activities/consultant-daily.component').then((m) => m.ConsultantDailyComponent)
      },
      {
        path: 'consultant/job-applications',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/job-applications/consultant-jobs.component').then((m) => m.ConsultantJobsComponent)
      },
      {
        path: 'consultant/vendor-reach-outs',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/vendor-reach-outs/consultant-vendor-reachouts.component').then(
            (m) => m.ConsultantVendorReachoutsComponent
          )
      },
      {
        path: 'consultant/submissions',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/submissions/consultant-submissions.component').then((m) => m.ConsultantSubmissionsComponent)
      },
      {
        path: 'consultant/interviews',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/interviews/consultant-interviews.component').then((m) => m.ConsultantInterviewsComponent)
      },
      {
        path: 'consultant/profile',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/profile/consultant-profile.component').then((m) => m.ConsultantProfileComponent)
      },
      {
        path: 'consultant/documents',
        canActivate: [roleGuard],
        data: { roles: ['Consultant', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./consultant/documents/consultant-documents.component').then((m) => m.ConsultantDocumentsComponent)
      },

      {
        path: 'sales/dashboard',
        canActivate: [roleGuard],
        data: { roles: ['SalesRecruiter', 'Admin', 'Management'] },
        loadComponent: () => import('./sales/dashboard/sales-dashboard.component').then((m) => m.SalesDashboardComponent)
      },
      {
        path: 'sales/assigned-consultants',
        canActivate: [roleGuard],
        data: { roles: ['SalesRecruiter', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./sales/assigned-consultants/sales-assigned.component').then((m) => m.SalesAssignedComponent)
      },
      {
        path: 'sales/vendors',
        canActivate: [roleGuard],
        data: { roles: ['SalesRecruiter', 'Admin', 'Management'] },
        loadComponent: () => import('./sales/vendors/sales-vendors.component').then((m) => m.SalesVendorsComponent)
      },
      {
        path: 'sales/submissions',
        canActivate: [roleGuard],
        data: { roles: ['SalesRecruiter', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./sales/submissions/sales-submissions.component').then((m) => m.SalesSubmissionsComponent)
      },
      {
        path: 'sales/interviews',
        canActivate: [roleGuard],
        data: { roles: ['SalesRecruiter', 'Admin', 'Management'] },
        loadComponent: () =>
          import('./sales/interviews/sales-interviews.component').then((m) => m.SalesInterviewsComponent)
      },

      {
        path: 'management/dashboard',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/dashboard/management-dashboard.component').then((m) => m.ManagementDashboardComponent)
      },
      {
        path: 'management/onboarding',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/onboarding/management-onboarding.component').then((m) => m.ManagementOnboardingComponent)
      },
      {
        path: 'management/documents',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/documents/management-documents.component').then((m) => m.ManagementDocumentsComponent)
      },
      {
        path: 'management/submissions',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/submissions/management-submissions.component').then((m) => m.ManagementSubmissionsComponent)
      },
      {
        path: 'management/interviews',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/interviews/management-interviews.component').then((m) => m.ManagementInterviewsComponent)
      },
      {
        path: 'management/reports',
        canActivate: [roleGuard],
        data: { roles: ['Management', 'Admin'] },
        loadComponent: () =>
          import('./management/reports/management-reports.component').then((m) => m.ManagementReportsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];

