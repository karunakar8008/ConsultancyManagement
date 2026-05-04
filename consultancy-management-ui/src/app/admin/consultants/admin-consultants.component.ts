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
  selector: 'app-admin-consultants',
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
  templateUrl: './admin-consultants.component.html',
  styleUrl: './admin-consultants.component.scss'
})
export class AdminConsultantsComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['userEmployeeId', 'name', 'email', 'technology', 'skillsNotes', 'visaStatus', 'experience', 'location', 'status', 'actions'];
  rows: Record<string, unknown>[] = [];
  consultantUserOptions: AdminUser[] = [];
  private allUsers: AdminUser[] = [];

  editingEmpId: string | null = null;

  form = this.fb.nonNullable.group({
    userId: ['', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    visaStatus: [''],
    technology: [''],
    skillsNotes: [''],
    experienceYears: [0 as number],
    currentLocation: [''],
    status: ['Active', Validators.required]
  });

  editForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    visaStatus: [''],
    technology: [''],
    skillsNotes: [''],
    experienceYears: [0 as number],
    currentLocation: [''],
    status: ['Active', Validators.required]
  });

  ngOnInit(): void {
    this.reload();
    this.admin.users().subscribe({
      next: (u) => {
        this.allUsers = u;
        this.refreshConsultantUserOptions();
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

  private refreshConsultantUserOptions(): void {
    const taken = new Set(
      this.rows.map((r) => String(r['userEmployeeId'] ?? '')).filter((x) => x.length > 0)
    );
    this.consultantUserOptions = this.allUsers.filter(
      (u) => u.roles.some((r) => r === 'Consultant') && !taken.has(u.id)
    );
  }

  reload(): void {
    this.admin.consultants().subscribe({
      next: (r) => {
        this.rows = r as Record<string, unknown>[];
        this.refreshConsultantUserOptions();
      },
      error: () => this.toast.error('Failed to load consultants')
    });
  }

  beginEdit(row: Record<string, unknown>): void {
    this.editingEmpId = String(row['userEmployeeId'] ?? '');
    this.editForm.reset({
      firstName: String(row['firstName'] ?? ''),
      lastName: String(row['lastName'] ?? ''),
      email: String(row['email'] ?? ''),
      phoneNumber: String(row['phoneNumber'] ?? ''),
      visaStatus: String(row['visaStatus'] ?? ''),
      technology: String(row['technology'] ?? ''),
      skillsNotes: String(row['skillsNotes'] ?? ''),
      experienceYears: Number(row['experienceYears'] ?? 0),
      currentLocation: String(row['currentLocation'] ?? ''),
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
      .updateConsultant(this.editingEmpId, {
        userId: this.editingEmpId,
        firstName: v.firstName,
        lastName: v.lastName,
        email: v.email,
        phoneNumber: v.phoneNumber,
        visaStatus: v.visaStatus,
        technology: v.technology,
        skillsNotes: v.skillsNotes,
        experienceYears: Number(v.experienceYears) || 0,
        currentLocation: v.currentLocation,
        status: v.status
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Consultant updated');
          this.editingEmpId = null;
          this.reload();
        },
        error: (err: { error?: { message?: string } }) => this.toast.error(err?.error?.message ?? 'Update failed')
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.admin
      .createConsultant({
        userId: raw.userId.trim(),
        firstName: raw.firstName,
        lastName: raw.lastName,
        email: raw.email,
        phoneNumber: raw.phoneNumber,
        visaStatus: raw.visaStatus,
        technology: raw.technology,
        skillsNotes: raw.skillsNotes,
        experienceYears: Number(raw.experienceYears) || 0,
        currentLocation: raw.currentLocation,
        status: raw.status
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Consultant profile created');
          this.form.reset({
            userId: '',
            firstName: '',
            lastName: '',
            email: '',
            phoneNumber: '',
            visaStatus: '',
            technology: '',
            skillsNotes: '',
            experienceYears: 0,
            currentLocation: '',
            status: 'Active'
          });
          this.reload();
        },
        error: (err: { error?: { message?: string } }) =>
          this.toast.error(err?.error?.message ?? 'Save failed')
      });
  }
}
