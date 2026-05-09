import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ConsultantInterviewRow } from '../../core/services/consultant-api.service';

export interface ConsultantInterviewEditDialogResult {
  interviewDate: string;
  interviewEndDate: string | null;
  interviewMode: string | null;
  round: string | null;
  status: string;
  feedback: string | null;
  notes: string | null;
}

function formatForDatetimeLocal(iso: string | undefined): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-consultant-interview-edit-dialog',
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
    <h2 mat-dialog-title>Edit interview</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline" class="wide">
          <mat-label>From (local)</mat-label>
          <input matInput type="datetime-local" formControlName="interviewDate" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="wide">
          <mat-label>To (local, optional)</mat-label>
          <input matInput type="datetime-local" formControlName="interviewEndDate" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Mode</mat-label>
          <input matInput formControlName="interviewMode" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Round</mat-label>
          <input matInput formControlName="round" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <input matInput formControlName="status" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="wide">
          <mat-label>Feedback</mat-label>
          <textarea matInput rows="3" formControlName="feedback"></textarea>
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
export class ConsultantInterviewEditDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastrService);
  private readonly ref = inject(
    MatDialogRef<ConsultantInterviewEditDialogComponent, ConsultantInterviewEditDialogResult | undefined>
  );
  private readonly row = inject<{ row: ConsultantInterviewRow }>(MAT_DIALOG_DATA).row;

  form = this.fb.nonNullable.group({
    interviewDate: ['', Validators.required],
    interviewEndDate: [''],
    interviewMode: [''],
    round: [''],
    status: ['', Validators.required],
    feedback: [''],
    notes: ['']
  });

  constructor() {
    this.form.patchValue({
      interviewDate: formatForDatetimeLocal(this.row.interviewDate),
      interviewEndDate: formatForDatetimeLocal(this.row.interviewEndDate ?? undefined),
      interviewMode: String(this.row.interviewMode ?? ''),
      round: String(this.row.round ?? ''),
      status: String(this.row.status ?? ''),
      feedback: String(this.row.feedback ?? ''),
      notes: String(this.row.notes ?? '')
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
    const startMs = new Date(v.interviewDate).getTime();
    const endLocal = v.interviewEndDate?.trim();
    const endMs = endLocal ? new Date(endLocal).getTime() : NaN;
    if (endLocal && !Number.isNaN(endMs) && endMs < startMs) {
      this.toast.error('End time must be after the start time.');
      return;
    }
    const trimOrNull = (s: string) => {
      const t = s.trim();
      return t ? t : null;
    };
    this.ref.close({
      interviewDate: new Date(v.interviewDate).toISOString(),
      interviewEndDate: endLocal && !Number.isNaN(endMs) ? new Date(endLocal).toISOString() : null,
      interviewMode: trimOrNull(v.interviewMode),
      round: trimOrNull(v.round),
      status: v.status.trim(),
      feedback: trimOrNull(v.feedback),
      notes: trimOrNull(v.notes)
    });
  }
}
