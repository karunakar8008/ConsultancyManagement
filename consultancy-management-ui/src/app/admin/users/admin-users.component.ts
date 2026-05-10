import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { AdminService } from '../../core/services/admin.service';
import { AdminUser } from '../../core/models/admin.models';
import { ToastrService } from 'ngx-toastr';
import { startWith } from 'rxjs';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule
  ],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  displayedColumns = ['id', 'name', 'email', 'phone', 'roles', 'status', 'actions'];
  users: AdminUser[] = [];
  roles: { name: string }[] = [];
  editingUserId: string | null = null;

  form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: ['', Validators.required],
    role: ['Consultant', Validators.required],
    employeeId: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    this.reload();
    this.admin.roles().subscribe((r) => (this.roles = r));

    this.form.controls.role.valueChanges
      .pipe(startWith(this.form.controls.role.value), takeUntilDestroyed(this.destroyRef))
      .subscribe((role) => this.refreshNextEmployeeId(role));
  }

  private refreshNextEmployeeId(role: string): void {
    if (this.editingUserId) return;
    this.admin.nextEmployeeId(role).subscribe({
      next: (x) => this.form.patchValue({ employeeId: x.employeeId }, { emitEvent: false }),
      error: () => {}
    });
  }

  reload(): void {
    this.admin.users().subscribe({
      next: (users) => (this.users = users),
      error: () => this.toast.error('Failed to load users')
    });
  }

  register(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    if (this.editingUserId) {
      const payload = {
        firstName: v.firstName,
        lastName: v.lastName,
        email: v.email,
        phoneNumber: v.phoneNumber,
        isActive: v.isActive,
        roles: [v.role]
      };
      this.admin.updateUser(this.editingUserId, payload).subscribe({
        next: () => {
          this.toast.success('User updated successfully');
          this.cancelEdit();
          this.reload();
        },
        error: (err: { error?: { message?: string } }) =>
          this.toast.error(err?.error?.message ?? 'Update failed')
      });
      return;
    }

    const body: Record<string, unknown> = {
      firstName: v.firstName,
      lastName: v.lastName,
      email: v.email,
      phoneNumber: v.phoneNumber,
      password: v.password,
      role: v.role
    };
    if (v.employeeId?.trim()) {
      body['employeeId'] = v.employeeId.trim();
    }

    this.admin.createUser(body).subscribe({
      next: () => {
        this.toast.success('User created successfully');
        this.form.reset({ role: 'Consultant', isActive: true });
        this.refreshNextEmployeeId(this.form.controls.role.value);
        this.reload();
      },
      error: (err: { error?: { message?: string } }) =>
        this.toast.error(err?.error?.message ?? 'Create failed')
    });
  }

  editUser(user: AdminUser): void {
    this.editingUserId = user.id;
    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber ?? '',
      password: '',
      role: user.roles[0] ?? 'Consultant',
      employeeId: user.id,
      isActive: user.isActive
    });
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
  }

  cancelEdit(): void {
    this.editingUserId = null;
    this.form.reset({ role: 'Consultant', isActive: true, password: '' });
    this.form.controls.password.setValidators([Validators.required]);
    this.form.controls.password.updateValueAndValidity();
    this.refreshNextEmployeeId(this.form.controls.role.value);
  }

  /** Tenant/org admins and platform operators cannot be removed from this screen. */
  canDeleteUser(user: AdminUser): boolean {
    const blocked = new Set(['Admin', 'PlatformAdmin']);
    return !user.roles.some((r) => blocked.has(r));
  }

  deleteUser(user: AdminUser): void {
    if (!this.canDeleteUser(user)) return;
    if (!user.id) {
      this.toast.error('Missing employee id');
      return;
    }
    if (!confirm(`Delete user ${user.email}?`)) return;

    this.admin.deleteUser(user.id).subscribe({
      next: () => {
        this.toast.success('User archived successfully');
        this.reload();
      },
      error: (err: { error?: { message?: string } }) =>
        this.toast.error(err?.error?.message ?? 'Delete failed')
    });
  }
}
