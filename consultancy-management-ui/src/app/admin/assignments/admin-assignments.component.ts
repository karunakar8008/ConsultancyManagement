import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AdminService } from '../../core/services/admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin-assignments',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatTableModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule
  ],
  templateUrl: './admin-assignments.component.html',
  styleUrl: '../sales-recruiters/admin-panel.scss'
})
export class AdminAssignmentsComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['consultant', 'sales', 'start', 'end', 'active', 'actions'];
  rows: Record<string, unknown>[] = [];

  smCols = ['sales', 'management', 'start', 'end', 'active', 'actions'];
  smRows: Record<string, unknown>[] = [];

  consultants: Record<string, unknown>[] = [];
  salesRecruiters: Record<string, unknown>[] = [];
  managementUsers: Record<string, unknown>[] = [];

  editingCsId: number | null = null;
  editingSmId: number | null = null;

  form = this.fb.nonNullable.group({
    consultantId: [0, [Validators.required, Validators.min(1)]],
    salesRecruiterId: [0, [Validators.required, Validators.min(1)]],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  smForm = this.fb.nonNullable.group({
    salesRecruiterId: [0, [Validators.required, Validators.min(1)]],
    managementUserId: [0, [Validators.required, Validators.min(1)]],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  editCs = this.fb.nonNullable.group({
    endDate: [''],
    isActive: [true]
  });

  editSm = this.fb.nonNullable.group({
    endDate: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    this.reload();
    this.reloadSalesMgmt();
    this.admin.consultants().subscribe({
      next: (c) => (this.consultants = c as Record<string, unknown>[]),
      error: () => {}
    });
    this.admin.salesRecruiters().subscribe({
      next: (s) => (this.salesRecruiters = s as Record<string, unknown>[]),
      error: () => {}
    });
    this.admin.managementUsers().subscribe({
      next: (m) => (this.managementUsers = m as Record<string, unknown>[]),
      error: () => {}
    });
  }

  reload(): void {
    this.admin.assignments().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load')
    });
  }

  reloadSalesMgmt(): void {
    this.admin.salesManagementAssignments().subscribe({
      next: (r) => (this.smRows = r as Record<string, unknown>[]),
      error: () => {}
    });
  }

  beginEditCs(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    this.editingCsId = id;
    const end = row['endDate'] ? new Date(String(row['endDate'])).toISOString().slice(0, 10) : '';
    this.editCs.reset({ endDate: end, isActive: Boolean(row['isActive']) });
  }

  cancelEditCs(): void {
    this.editingCsId = null;
  }

  saveEditCs(): void {
    if (this.editingCsId == null) return;
    const v = this.editCs.getRawValue();
    const endDate = v.endDate ? new Date(v.endDate).toISOString() : null;
    this.admin.updateAssignment(this.editingCsId, { endDate, isActive: v.isActive }).subscribe({
      next: () => {
        this.toast.success('Assignment updated');
        this.editingCsId = null;
        this.reload();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
    });
  }

  beginEditSm(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    this.editingSmId = id;
    const end = row['endDate'] ? new Date(String(row['endDate'])).toISOString().slice(0, 10) : '';
    this.editSm.reset({ endDate: end, isActive: Boolean(row['isActive']) });
  }

  cancelEditSm(): void {
    this.editingSmId = null;
  }

  saveEditSm(): void {
    if (this.editingSmId == null) return;
    const v = this.editSm.getRawValue();
    const endDate = v.endDate ? new Date(v.endDate).toISOString() : null;
    this.admin.updateSalesManagementAssignment(this.editingSmId, { endDate, isActive: v.isActive }).subscribe({
      next: () => {
        this.toast.success('Assignment updated');
        this.editingSmId = null;
        this.reloadSalesMgmt();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
    });
  }

  save(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const v = this.form.getRawValue();
    this.admin
      .createAssignment({
        consultantId: Number(v.consultantId),
        salesRecruiterId: Number(v.salesRecruiterId),
        startDate: new Date(v.startDate).toISOString()
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Consultant assigned successfully');
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }

  saveSalesMgmt(): void {
    if (this.smForm.invalid) return this.smForm.markAllAsTouched();
    const v = this.smForm.getRawValue();
    this.admin
      .createSalesManagementAssignment({
        salesRecruiterId: Number(v.salesRecruiterId),
        managementUserId: Number(v.managementUserId),
        startDate: new Date(v.startDate).toISOString()
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Sales to management assignment created');
          this.reloadSalesMgmt();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
