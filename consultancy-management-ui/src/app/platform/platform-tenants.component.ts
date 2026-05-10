import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { OrganizationListItem, PlatformService } from '../core/services/platform.service';
import {
  BootstrapOrgAdminDialogComponent,
  BootstrapOrgAdminDialogResult
} from './bootstrap-org-admin-dialog.component';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-platform-tenants',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatDialogModule,
    MatChipsModule
  ],
  templateUrl: './platform-tenants.component.html',
  styleUrl: './platform-tenants.component.scss'
})
export class PlatformTenantsComponent implements OnInit {
  private readonly platform = inject(PlatformService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  displayedColumns = ['name', 'slug', 'status', 'actions'];
  loading = false;
  creating = false;
  organizations: OrganizationListItem[] = [];

  createForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    slug: [
      '',
      [Validators.required, Validators.pattern(/^[a-z0-9]([a-z0-9-]*[a-z0-9])?$/)]
    ]
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading = true;
    this.platform
      .listOrganizations()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (list) => (this.organizations = list),
        error: () => this.toast.error('Failed to load organizations')
      });
  }

  createTenant(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const { name, slug } = this.createForm.getRawValue();
    this.creating = true;
    this.platform
      .createOrganization({ name: name.trim(), slug: slug.trim().toLowerCase() })
      .pipe(finalize(() => (this.creating = false)))
      .subscribe({
        next: () => {
          this.toast.success('Organization created');
          this.createForm.reset({ name: '', slug: '' });
          this.reload();
        },
        error: (err: { error?: { message?: string } }) =>
          this.toast.error(err?.error?.message ?? 'Could not create organization')
      });
  }

  openBootstrapAdmin(org: { id: number; name: string; slug: string }): void {
    const ref = this.dialog.open(BootstrapOrgAdminDialogComponent, {
      width: '480px',
      data: {
        organizationId: org.id,
        organizationName: org.name,
        slug: org.slug
      }
    });
    ref.afterClosed().subscribe((result: BootstrapOrgAdminDialogResult | undefined) => {
      if (!result) return;
      this.platform.bootstrapOrganizationAdmin(org.id, result).subscribe({
        next: () => {
          this.toast.success('Tenant admin created. They can sign in with this organization code.');
          this.reload();
        },
        error: (err: { error?: { message?: string } }) =>
          this.toast.error(err?.error?.message ?? 'Could not create admin')
      });
    });
  }
}
