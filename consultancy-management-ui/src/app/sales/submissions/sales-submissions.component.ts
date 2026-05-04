import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin } from 'rxjs';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import {
  AssignedConsultantOption,
  SalesService,
  SalesSubmissionRow,
  VendorRow
} from '../../core/services/sales.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-sales-submissions',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './sales-submissions.component.html',
  styleUrls: ['./sales-submissions.component.scss', '../../consultant/daily-activities/consultant-shared.scss']
})
export class SalesSubmissionsComponent implements OnInit {
  private readonly api = inject(SalesService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['code', 'consultant', 'vendor', 'job', 'client', 'date', 'status', 'proof', 'actions'];
  rows: SalesSubmissionRow[] = [];
  consultants: AssignedConsultantOption[] = [];
  vendors: VendorRow[] = [];
  editingId: number | null = null;
  proofFile: File | null = null;

  form = this.fb.nonNullable.group({
    consultantId: [0, [Validators.required, Validators.min(1)]],
    vendorId: [0, [Validators.required, Validators.min(1)]],
    jobTitle: ['', Validators.required],
    clientName: [''],
    submissionDate: [new Date().toISOString().slice(0, 10), Validators.required],
    status: ['Submitted'],
    rate: [null as number | null],
    notes: ['']
  });

  ngOnInit(): void {
    this.reloadLists();
    this.reload();
  }

  reloadLists(): void {
    forkJoin({
      consultants: this.api.assignedConsultants(),
      vendors: this.api.vendors()
    }).subscribe({
      next: ({ consultants, vendors }) => {
        this.consultants = consultants;
        this.vendors = vendors;
      },
      error: () => this.toast.error('Failed to load dropdown data')
    });
  }

  reload(): void {
    this.api.submissions().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load submissions')
    });
  }

  onProof(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    this.proofFile = input.files?.[0] ?? null;
  }

  cancelEdit(): void {
    this.editingId = null;
    this.proofFile = null;
    this.form.reset({
      consultantId: 0,
      vendorId: 0,
      jobTitle: '',
      clientName: '',
      submissionDate: new Date().toISOString().slice(0, 10),
      status: 'Submitted',
      rate: null,
      notes: ''
    });
  }

  startEdit(row: SalesSubmissionRow): void {
    this.editingId = row.id;
    this.proofFile = null;
    const d = row.submissionDate ? new Date(row.submissionDate) : new Date();
    this.form.patchValue({
      consultantId: row.consultantId,
      vendorId: row.vendorId,
      jobTitle: row.jobTitle,
      clientName: row.clientName ?? '',
      submissionDate: d.toISOString().slice(0, 10),
      status: row.status,
      rate: row.rate ?? null,
      notes: row.notes ?? ''
    });
  }

  viewProof(row: SalesSubmissionRow): void {
    this.api.downloadProof('submission', row.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(row: SalesSubmissionRow): void {
    this.api.downloadProof('submission', row.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${row.submissionCode}-proof`),
      error: () => this.toast.error('Download failed')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.editingId == null && !this.proofFile) {
      this.toast.error('Submission proof file is required for new submissions.');
      return;
    }
    const v = this.form.getRawValue();
    const payload = {
      consultantId: v.consultantId,
      vendorId: v.vendorId,
      jobTitle: v.jobTitle.trim(),
      clientName: v.clientName.trim(),
      submissionDate: v.submissionDate,
      status: v.status.trim(),
      rate: v.rate,
      notes: v.notes.trim()
    };

    if (this.editingId != null) {
      this.api.updateSubmission(this.editingId, payload, this.proofFile).subscribe({
        next: () => {
          this.toast.success('Submission updated');
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Update failed')
      });
    } else {
      this.api.createSubmission(payload, this.proofFile!).subscribe({
        next: () => {
          this.toast.success('Submission created');
          this.proofFile = null;
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Create failed')
      });
    }
  }
}
