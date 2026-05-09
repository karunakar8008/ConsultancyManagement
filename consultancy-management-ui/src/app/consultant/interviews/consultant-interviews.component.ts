import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ConsultantApiService, ConsultantInterviewRow } from '../../core/services/consultant-api.service';
import { ToastrService } from 'ngx-toastr';
import {
  ConsultantInterviewEditDialogComponent,
  ConsultantInterviewEditDialogResult
} from '../shared/consultant-interview-edit-dialog.component';

@Component({
  selector: 'app-consultant-interviews',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './consultant-interviews.component.html',
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
export class ConsultantInterviewsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly dialog = inject(MatDialog);

  cols = ['code', 'sub', 'job', 'date', 'to', 'mode', 'round', 'status', 'feedback', 'notes', 'edit', 'proof'];
  rows: ConsultantInterviewRow[] = [];

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.interviews().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load interviews')
    });
  }

  viewProof(r: ConsultantInterviewRow): void {
    this.api.downloadProof('interview', r.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(r: ConsultantInterviewRow): void {
    this.api.downloadProof('interview', r.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${r.interviewCode}-invite`),
      error: () => this.toast.error('Download failed')
    });
  }

  textOrDash(value: string | null | undefined): string {
    const t = value?.trim();
    return t ? t : '—';
  }

  openEdit(r: ConsultantInterviewRow): void {
    const ref = this.dialog.open(ConsultantInterviewEditDialogComponent, {
      width: 'min(560px, 94vw)',
      data: { row: r }
    });
    ref.afterClosed().subscribe((result: ConsultantInterviewEditDialogResult | undefined) => {
      if (!result) return;
      this.api.updateInterview(r.id, result).subscribe({
        next: () => {
          this.toast.success('Interview updated');
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Update failed')
      });
    });
  }
}
