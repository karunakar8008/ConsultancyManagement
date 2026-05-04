import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { AdminService } from '../../core/services/admin.service';
import { AdminUser } from '../../core/models/admin.models';
import { ToastrService } from 'ngx-toastr';
import { distinctUntilChanged, filter } from 'rxjs';

@Component({
  selector: 'app-admin-management-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule
  ],
  templateUrl: './admin-management-users.component.html',
  styleUrl: '../sales-recruiters/admin-panel.scss'
})
export class AdminManagementUsersComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  cols = ['userEmployeeId', 'name', 'email', 'department', 'status', 'actions'];
  rows: Record<string, unknown>[] = [];
  userOptions: AdminUser[] = [];
  private allUsers: AdminUser[] = [];
  editingEmpId: string | null = null;

  form = this.fb.nonNullable.group({
    userId: ['', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    department: [''],
    status: ['Active', Validators.required]
  });

  editForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    department: [''],
    status: ['Active', Validators.required]
  });

  ngOnInit(): void {
    this.reload();
    this.admin.users().subscribe({
      next: (u) => {
        this.allUsers = u;
        this.refreshUserOptions();
      },
      error: () => this.toast.error('Failed to load users')
    });

    this.form.controls.userId.valueChanges
      .pipe(
        filter((id): id is string => typeof id === 'string' && id.trim().length > 0),
        distinctUntilChanged()
      )
      .subscribe((employeeId) => {
        this.admin.userById(employeeId.trim()).subscribe({
          next: (detail) => {
            this.form.patchValue(
              {
                firstName: detail.firstName,
                lastName: detail.lastName,
                email: detail.email,
                phoneNumber: detail.phoneNumber ?? ''
              },
              { emitEvent: false }
            );
          },
          error: () => {}
        });
      });
  }

  private refreshUserOptions(): void {
    const taken = new Set(
      this.rows.map((r) => String(r['userEmployeeId'] ?? '')).filter((x) => x.length > 0)
    );
    this.userOptions = this.allUsers.filter(
      (u) => u.roles.some((r) => r === 'Management') && !taken.has(u.id)
    );
  }

  reload(): void {
    this.admin.managementUsers().subscribe({
      next: (r) => {
        this.rows = r as Record<string, unknown>[];
        this.refreshUserOptions();
      },
      error: () => this.toast.error('Failed to load')
    });
  }

  beginEdit(row: Record<string, unknown>): void {
    this.editingEmpId = String(row['userEmployeeId'] ?? '');
    this.editForm.reset({
      firstName: String(row['firstName'] ?? ''),
      lastName: String(row['lastName'] ?? ''),
      email: String(row['email'] ?? ''),
      phoneNumber: String(row['phoneNumber'] ?? ''),
      department: String(row['department'] ?? ''),
      status: String(row['status'] ?? 'Active')
    });
  }

  cancelEdit(): void {
    this.editingEmpId = null;
  }

  saveEdit(): void {
    if (this.editingEmpId == null || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    const v = this.editForm.getRawValue();
    this.admin
      .updateManagement(this.editingEmpId, {
        userId: this.editingEmpId,
        firstName: v.firstName,
        lastName: v.lastName,
        email: v.email,
        phoneNumber: v.phoneNumber,
        department: v.department,
        status: v.status
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Updated');
          this.editingEmpId = null;
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }

  save(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const v = this.form.getRawValue();
    this.admin
      .createManagementUser({
        userId: v.userId.trim(),
        firstName: v.firstName,
        lastName: v.lastName,
        email: v.email,
        phoneNumber: v.phoneNumber,
        department: v.department,
        status: v.status
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Saved');
          this.form.reset({
            status: 'Active',
            userId: '',
            firstName: '',
            lastName: '',
            email: '',
            phoneNumber: '',
            department: ''
          });
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
