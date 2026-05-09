import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ConsultantJobEditDialogResult {
  jobTitle: string;
  companyName: string | null;
  clientName: string | null;
  source: string | null;
  jobUrl: string | null;
  appliedDate: string;
  status: string;
  notes: string | null;
}

@Component({
  selector: 'app-consultant-job-edit-dialog',
  standalone: true,
  imports: [MatDialogModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  styles: [
    `
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 0.75rem;
        padding-top: 0.25rem;
      }
      .wide {
        grid-column: 1 / -1;
      }
      mat-dialog-content {
        min-width: min(520px, 92vw);
      }
    `
  ],
  template: `
    <h2 mat-dialog-title>Edit job application</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="jobTitle" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Company</mat-label>
          <input matInput formControlName="companyName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Client</mat-label>
          <input matInput formControlName="clientName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Source</mat-label>
          <input matInput formControlName="source" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="wide">
          <mat-label>URL</mat-label>
          <input matInput formControlName="jobUrl" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Applied</mat-label>
          <input matInput type="date" formControlName="appliedDate" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <input matInput formControlName="status" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="wide">
          <mat-label>Notes</mat-label>
          <textarea matInput rows="3" formControlName="notes"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancel</button>
      <button mat-flat-button color="primary" type="button" (click)="save()">Save</button>
    </mat-dialog-actions>
  `
})
export class ConsultantJobEditDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(
    MatDialogRef<ConsultantJobEditDialogComponent, ConsultantJobEditDialogResult | undefined>
  );
  private readonly row = inject<{ row: Record<string, unknown> }>(MAT_DIALOG_DATA).row;

  form = this.fb.nonNullable.group({
    jobTitle: ['', Validators.required],
    companyName: [''],
    clientName: [''],
    source: [''],
    jobUrl: [''],
    appliedDate: ['', Validators.required],
    status: ['Applied'],
    notes: ['']
  });

  constructor() {
    const appliedRaw = this.row['appliedDate'];
    let applied = '';
    if (appliedRaw != null) {
      const d = new Date(String(appliedRaw));
      applied = Number.isNaN(d.getTime()) ? '' : d.toISOString().slice(0, 10);
    }
    this.form.patchValue({
      jobTitle: String(this.row['jobTitle'] ?? ''),
      companyName: String(this.row['companyName'] ?? ''),
      clientName: String(this.row['clientName'] ?? ''),
      source: String(this.row['source'] ?? ''),
      jobUrl: String(this.row['jobUrl'] ?? ''),
      appliedDate: applied,
      status: String(this.row['status'] ?? 'Applied'),
      notes: String(this.row['notes'] ?? '')
    });
  }

  cancel(): void {
    this.ref.close(undefined);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const trimOrNull = (s: string) => {
      const t = s.trim();
      return t ? t : null;
    };
    this.ref.close({
      jobTitle: v.jobTitle.trim(),
      companyName: trimOrNull(v.companyName),
      clientName: trimOrNull(v.clientName),
      source: trimOrNull(v.source),
      jobUrl: trimOrNull(v.jobUrl),
      appliedDate: new Date(v.appliedDate).toISOString(),
      status: v.status.trim() || 'Applied',
      notes: trimOrNull(v.notes)
    });
  }
}
