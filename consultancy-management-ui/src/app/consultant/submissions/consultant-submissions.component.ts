import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { ConsultantApiService, ConsultantSubmissionRow } from '../../core/services/consultant-api.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';
import { ConsultantTextDialogComponent } from '../shared/consultant-text-dialog.component';

@Component({
  selector: 'app-consultant-submissions',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './consultant-submissions.component.html',
  styles: [
    `
      .panel {
        padding: 1rem;
      }
      .full-table {
        width: 100%;
      }
      .hint {
        color: rgba(0, 0, 0, 0.65);
        margin: 0 0 0.75rem;
        font-size: 0.9rem;
      }
      .icon-actions {
        display: inline-flex;
        gap: 0.15rem;
        align-items: center;
      }
      .text-cell {
        max-width: 14rem;
        white-space: pre-wrap;
        word-break: break-word;
        font-size: 0.875rem;
        vertical-align: top;
      }
    `
  ]
})
export class ConsultantSubmissionsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly dialog = inject(MatDialog);

  cols = ['code', 'job', 'client', 'vendor', 'sales', 'date', 'status', 'notes', 'comm', 'edit', 'proof'];
  rows: ConsultantSubmissionRow[] = [];

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.submissions().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load')
    });
  }

  viewProof(r: ConsultantSubmissionRow): void {
    this.api.downloadProof('submission', r.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(r: ConsultantSubmissionRow): void {
    this.api.downloadProof('submission', r.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${r.submissionCode}-proof`),
      error: () => this.toast.error('Download failed')
    });
  }

  textOrDash(value: string | null | undefined): string {
    const t = value?.trim();
    return t ? t : '—';
  }

  editCommunication(r: ConsultantSubmissionRow): void {
    const ref = this.dialog.open(ConsultantTextDialogComponent, {
      width: 'min(480px, 94vw)',
      data: {
        title: 'Communication with vendor',
        label: 'Notes on follow-up with the submitted vendor',
        value: r.consultantCommunication ?? '',
        rows: 6
      }
    });
    ref.afterClosed().subscribe((text: string | undefined) => {
      if (text === undefined) return;
      const consultantCommunication = text.trim() ? text.trim() : null;
      this.api.updateSubmissionCommunication(r.id, { consultantCommunication }).subscribe({
        next: () => {
          this.toast.success('Saved');
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Could not save')
      });
    });
  }
}
