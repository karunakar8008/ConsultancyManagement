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
import { InterviewRow, SalesService, SubmissionOption } from '../../core/services/sales.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-sales-interviews',
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
  templateUrl: './sales-interviews.component.html',
  styleUrls: ['./sales-interviews.component.scss', '../../consultant/daily-activities/consultant-shared.scss']
})
export class SalesInterviewsComponent implements OnInit {
  private readonly api = inject(SalesService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = [
    'code',
    'submission',
    'consultant',
    'job',
    'from',
    'to',
    'duration',
    'mode',
    'round',
    'status',
    'feedback',
    'notes',
    'proof',
    'actions'
  ];
  rows: InterviewRow[] = [];
  submissionChoices: SubmissionOption[] = [];
  editingId: number | null = null;
  inviteFile: File | null = null;

  form = this.fb.nonNullable.group({
    submissionId: [0, [Validators.required, Validators.min(1)]],
    interviewDate: [this.defaultLocalDatetime(), Validators.required],
    interviewEndDate: [''],
    interviewMode: ['Virtual'],
    round: ['1'],
    status: ['Scheduled'],
    feedback: [''],
    notes: ['']
  });

  ngOnInit(): void {
    forkJoin({ interviews: this.api.interviews(), options: this.api.submissionOptions() }).subscribe({
      next: ({ interviews, options }) => {
        this.rows = interviews;
        this.submissionChoices = options;
      },
      error: () => this.toast.error('Failed to load interviews')
    });
  }

  private defaultLocalDatetime(): string {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }

  reload(): void {
    this.api.interviews().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load interviews')
    });
    this.api.submissionOptions().subscribe({
      next: (o) => (this.submissionChoices = o),
      error: () => {}
    });
  }

  onInvite(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    this.inviteFile = input.files?.[0] ?? null;
  }

  cancelEdit(): void {
    this.editingId = null;
    this.inviteFile = null;
    this.form.reset({
      submissionId: 0,
      interviewDate: this.defaultLocalDatetime(),
      interviewEndDate: '',
      interviewMode: 'Virtual',
      round: '1',
      status: 'Scheduled',
      feedback: '',
      notes: ''
    });
  }

  startEdit(row: InterviewRow): void {
    this.editingId = row.id;
    this.inviteFile = null;
    const d = row.interviewDate ? new Date(row.interviewDate) : new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    let endLocal = '';
    if (row.interviewEndDate) {
      const e = new Date(row.interviewEndDate);
      e.setMinutes(e.getMinutes() - e.getTimezoneOffset());
      endLocal = e.toISOString().slice(0, 16);
    }
    this.form.patchValue({
      submissionId: row.submissionId,
      interviewDate: d.toISOString().slice(0, 16),
      interviewEndDate: endLocal,
      interviewMode: row.interviewMode ?? 'Virtual',
      round: row.round ?? '1',
      status: row.status,
      feedback: row.feedback ?? '',
      notes: row.notes ?? ''
    });
  }

  viewProof(row: InterviewRow): void {
    this.api.downloadProof('interview', row.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(row: InterviewRow): void {
    this.api.downloadProof('interview', row.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${row.interviewCode}-invite`),
      error: () => this.toast.error('Download failed')
    });
  }

  /** Show placeholder only when empty so entered feedback/notes stay readable in the grid. */
  textOrDash(value: string | null | undefined): string {
    const t = value?.trim();
    return t ? t : '—';
  }

  durationLabel(r: InterviewRow): string {
    if (!r.interviewEndDate) return '—';
    const a = new Date(r.interviewDate).getTime();
    const b = new Date(r.interviewEndDate).getTime();
    if (!(b > a)) return '—';
    const mins = Math.round((b - a) / 60000);
    if (mins < 60) return `${mins} min`;
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return m ? `${h}h ${m}m` : `${h}h`;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.editingId == null && !this.inviteFile) {
      this.toast.error('Interview invite proof file is required.');
      return;
    }
    const v = this.form.getRawValue();
    const startMs = new Date(v.interviewDate).getTime();
    const endMs = v.interviewEndDate?.trim() ? new Date(v.interviewEndDate).getTime() : NaN;
    if (!Number.isNaN(endMs) && endMs < startMs) {
      this.toast.error('End time must be after the start time.');
      return;
    }
    const payload = {
      submissionId: v.submissionId,
      interviewDate: v.interviewDate,
      interviewEndDate: v.interviewEndDate?.trim() ?? '',
      interviewMode: v.interviewMode.trim(),
      round: v.round.trim(),
      status: v.status.trim(),
      feedback: v.feedback.trim(),
      notes: v.notes.trim()
    };

    if (this.editingId != null) {
      this.api.updateInterview(this.editingId, payload, this.inviteFile).subscribe({
        next: () => {
          this.toast.success('Interview updated');
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Update failed')
      });
    } else {
      this.api.createInterview(payload, this.inviteFile!).subscribe({
        next: () => {
          this.toast.success('Interview scheduled');
          this.inviteFile = null;
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Schedule failed')
      });
    }
  }
}
