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
import { ConsultantApiService } from '../../core/services/consultant-api.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-consultant-documents',
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
  templateUrl: './consultant-documents.component.html',
  styleUrl: '../daily-activities/consultant-shared.scss'
})
export class ConsultantDocumentsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['type', 'file', 'uploaded', 'status', 'actions'];
  rows: Record<string, unknown>[] = [];
  file?: File;

  form = this.fb.nonNullable.group({
    documentType: ['Resume', Validators.required]
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.documents().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load documents')
    });
  }

  onFile(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.file = input.files?.[0];
  }

  upload(): void {
    if (this.form.invalid || !this.file) {
      this.form.markAllAsTouched();
      if (!this.file) this.toast.error('Choose a file');
      return;
    }
    const fd = new FormData();
    fd.append('documentType', this.form.controls.documentType.value);
    fd.append('file', this.file, this.file.name);
    this.api.uploadDocument(fd).subscribe({
      next: () => {
        this.toast.success('Document uploaded');
        this.file = undefined;
        this.reload();
      },
      error: (err: { error?: { message?: string } }) =>
        this.toast.error(err?.error?.message ?? 'Upload failed')
    });
  }

  viewDoc(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    this.api.downloadDocument(id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadDoc(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    const fileName = String(row['fileName'] ?? 'download');
    this.api.downloadDocument(id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, fileName),
      error: () => this.toast.error('Download failed')
    });
  }
}
