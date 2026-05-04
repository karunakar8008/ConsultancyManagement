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
import { ManagementFileCatalogItem, ManagementService } from '../../core/services/management.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';

interface ConsultantOption {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

@Component({
  selector: 'app-management-documents',
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
  templateUrl: './management-documents.component.html',
  styles: [
    `
      .panel {
        padding: 1rem;
        margin-bottom: 1rem;
      }
      .grid-form {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
      }
      .full-table {
        width: 100%;
      }
      .wide {
        min-width: 220px;
        flex: 1 1 220px;
      }
      h3 {
        margin-top: 0;
      }
      .hint {
        color: rgba(0, 0, 0, 0.65);
        font-size: 0.9rem;
        margin: 0 0 0.75rem;
        line-height: 1.45;
      }
      .file-row {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
      }
      .file-label {
        font-size: 0.85rem;
        font-weight: 500;
      }
      .icon-actions {
        display: inline-flex;
        gap: 0.15rem;
        align-items: center;
      }
    `
  ]
})
export class ManagementDocumentsComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  cols = ['consultant', 'type', 'file', 'uploaded', 'status', 'fileActions'];
  catalogCols = ['destination', 'kind', 'context', 'file', 'date', 'actions'];
  rows: Record<string, unknown>[] = [];
  catalog: ManagementFileCatalogItem[] = [];
  consultants: ConsultantOption[] = [];
  mgmtFile?: File;

  reviewForm = this.fb.nonNullable.group({ id: [0], status: ['Approved'] });

  uploadForm = this.fb.nonNullable.group({
    consultantId: [0, [Validators.required, Validators.min(1)]],
    documentType: ['OfferLetter', Validators.required]
  });

  ngOnInit(): void {
    this.reloadAll();
  }

  reloadAll(): void {
    forkJoin({
      docs: this.api.documents(),
      consultants: this.api.consultants(),
      catalog: this.api.fileCatalog()
    }).subscribe({
      next: ({ docs, consultants, catalog }) => {
        this.rows = docs as Record<string, unknown>[];
        this.consultants = consultants as ConsultantOption[];
        this.catalog = catalog;
      },
      error: () => this.toast.error('Failed to load')
    });
  }

  kindLabel(kind: string): string {
    switch (kind) {
      case 'Document':
        return 'Consultant document';
      case 'VendorContactProof':
        return 'Vendor contact proof';
      case 'SubmissionProof':
        return 'Submission proof';
      case 'InterviewInviteProof':
        return 'Interview invite';
      default:
        return kind;
    }
  }

  viewCatalogItem(item: ManagementFileCatalogItem): void {
    if (!item.hasFile) return;
    this.api.downloadCatalogFile(item.kind, item.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadCatalogItem(item: ManagementFileCatalogItem): void {
    if (!item.hasFile) return;
    this.api.downloadCatalogFile(item.kind, item.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, item.fileName || 'download'),
      error: () => this.toast.error('Download failed')
    });
  }

  viewQueueDoc(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    this.api.downloadConsultantPortalDocument(id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadQueueDoc(row: Record<string, unknown>): void {
    const id = Number(row['id']);
    const name = String(row['fileName'] ?? 'download');
    this.api.downloadConsultantPortalDocument(id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, name),
      error: () => this.toast.error('Download failed')
    });
  }

  onMgmtFile(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.mgmtFile = input.files?.[0];
  }

  upload(): void {
    if (this.uploadForm.invalid) {
      this.uploadForm.markAllAsTouched();
      return;
    }
    if (!this.mgmtFile) {
      this.toast.error('Choose a file');
      return;
    }
    const v = this.uploadForm.getRawValue();
    const fd = new FormData();
    fd.append('documentType', v.documentType);
    fd.append('file', this.mgmtFile, this.mgmtFile.name);
    this.api.uploadConsultantDocument(v.consultantId, fd).subscribe({
      next: () => {
        this.toast.success('Uploaded');
        this.mgmtFile = undefined;
        this.reloadAll();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Upload failed')
    });
  }

  review(): void {
    const v = this.reviewForm.getRawValue();
    this.api.reviewDocument(Number(v.id), { status: v.status }).subscribe({
      next: () => {
        this.toast.success('Updated');
        this.reloadAll();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
    });
  }
}
