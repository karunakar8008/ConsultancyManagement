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
import { MatExpansionModule } from '@angular/material/expansion';
import { forkJoin } from 'rxjs';
import { ManagementFileCatalogItem, ManagementService } from '../../core/services/management.service';
import { AuthService } from '../../core/services/auth.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';

interface ConsultantOption {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

interface ConsultantFolderCatalogGroup {
  folder: string;
  consultantName: string;
  /** Kind === Document — stored under <code>uploads/…/documents/</code> (new uploads). */
  documentItems: ManagementFileCatalogItem[];
  /** Vendor, submission, interview proofs — stored under <code>uploads/…/proofs/</code>. */
  proofItems: ManagementFileCatalogItem[];
}

interface ConsultantFolderReviewGroup {
  consultantId: number;
  folder: string;
  consultantName: string;
  rows: Record<string, unknown>[];
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
    MatTooltipModule,
    MatExpansionModule
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
      .folder-accordion .mat-expansion-panel {
        margin-bottom: 0.5rem;
      }
      .nested-html-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.875rem;
      }
      .nested-html-table th,
      .nested-html-table td {
        padding: 0.5rem 0.65rem;
        border-bottom: 1px solid rgba(0, 0, 0, 0.08);
        text-align: left;
        vertical-align: middle;
      }
      .nested-html-table th {
        font-weight: 600;
        background: rgba(0, 0, 0, 0.03);
      }
      code {
        font-size: 0.85em;
        padding: 0.1rem 0.25rem;
        background: rgba(0, 0, 0, 0.05);
        border-radius: 4px;
      }
      .lock-icon {
        font-size: 16px;
        width: 18px;
        height: 18px;
        vertical-align: middle;
        margin-left: 0.15rem;
        opacity: 0.75;
      }
      .subfolder-heading {
        margin: 1rem 0 0.5rem;
        font-size: 0.95rem;
        font-weight: 600;
      }
      .subfolder-heading:first-of-type {
        margin-top: 0;
      }
      .hint.tight {
        margin-top: 0;
      }
    `
  ]
})
export class ManagementDocumentsComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  cols = ['consultant', 'type', 'file', 'uploaded', 'status', 'fileActions'];
  rows: Record<string, unknown>[] = [];
  catalog: ManagementFileCatalogItem[] = [];
  catalogGroups: ConsultantFolderCatalogGroup[] = [];
  reviewGroups: ConsultantFolderReviewGroup[] = [];
  consultants: ConsultantOption[] = [];
  mgmtFile?: File;

  readonly reviewStatuses = ['Approved', 'Rejected', 'Re-check'] as const;

  reviewForm = this.fb.nonNullable.group({ id: [0], status: ['Approved' as string] });

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
        this.rebuildFolderGroups();
      },
      error: (err: unknown) => {
        const http = err as { error?: { message?: string } | string; message?: string };
        const body = http.error;
        const msg =
          typeof body === 'string'
            ? body
            : body && typeof body === 'object' && 'message' in body
              ? String((body as { message?: string }).message)
              : http.message;
        this.toast.error(msg ? `Failed to load: ${msg}` : 'Failed to load');
      }
    });
  }

  private rebuildFolderGroups(): void {
    const byFolder = new Map<string, { consultantName: string; items: ManagementFileCatalogItem[] }>();
    for (const item of this.catalog) {
      const folder = item.consultantStorageFolder?.trim() || '_legacy';
      let g = byFolder.get(folder);
      if (!g) {
        g = { consultantName: item.consultantName ?? '', items: [] };
        byFolder.set(folder, g);
      }
      g.items.push(item);
    }
    const sortItems = (xs: ManagementFileCatalogItem[]) =>
      [...xs].sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
    this.catalogGroups = [...byFolder.entries()]
      .map(([folder, v]) => ({
        folder,
        consultantName: v.consultantName,
        documentItems: sortItems(v.items.filter((i) => i.kind === 'Document')),
        proofItems: sortItems(v.items.filter((i) => i.kind !== 'Document'))
      }))
      .sort((a, b) => a.consultantName.localeCompare(b.consultantName));

    const reviewByConsultant = new Map<
      number,
      { consultantName: string; folder: string; rows: Record<string, unknown>[] }
    >();
    for (const row of this.rows) {
      const cid = Number(row['consultantId']);
      const folder = String(row['storageFolder'] ?? '_legacy').trim() || '_legacy';
      const name = String(row['consultantName'] ?? '');
      let g = reviewByConsultant.get(cid);
      if (!g) {
        g = { consultantName: name, folder, rows: [] };
        reviewByConsultant.set(cid, g);
      }
      g.rows.push(row);
    }
    this.reviewGroups = [...reviewByConsultant.entries()]
      .map(([consultantId, v]) => ({
        consultantId,
        folder: v.folder,
        consultantName: v.consultantName,
        rows: v.rows
      }))
      .sort((a, b) => a.consultantName.localeCompare(b.consultantName));
  }

  catalogTrack(item: ManagementFileCatalogItem): string {
    return `${item.kind}-${item.id}`;
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

  /** Options for the review form: document id + short label. */
  get reviewDocumentOptions(): { id: number; label: string }[] {
    return [...this.rows]
      .map((r) => {
        const id = Number(r['id']);
        const type = String(r['documentType'] ?? '');
        const file = String(r['fileName'] ?? '');
        const who = String(r['consultantName'] ?? '');
        return {
          id,
          label: `#${id} — ${type} — ${file}${who ? ` (${who})` : ''}`
        };
      })
      .filter((x) => Number.isFinite(x.id) && x.id > 0)
      .sort((a, b) => b.id - a.id);
  }

  /** Disable submit when nothing selected, or document locked by admin and user is not admin. */
  reviewSubmitDisabled(): boolean {
    const id = Number(this.reviewForm.getRawValue().id);
    if (!Number.isFinite(id) || id < 1) return true;
    const row = this.rows.find((r) => Number(r['id']) === id);
    if (!row) return true;
    const locked = Boolean(row['lockedAfterAdminDecision']);
    if (!locked) return false;
    return !this.auth.hasRole('Admin');
  }

  showLockedByAdminHint(): boolean {
    const id = Number(this.reviewForm.getRawValue().id);
    if (!Number.isFinite(id) || id < 1) return false;
    const row = this.rows.find((r) => Number(r['id']) === id);
    if (!row) return false;
    return Boolean(row['lockedAfterAdminDecision']) && !this.auth.hasRole('Admin');
  }

  review(): void {
    const v = this.reviewForm.getRawValue();
    if (!v.id || v.id < 1) {
      this.toast.warning('Select a document from the list.');
      return;
    }
    if (this.reviewSubmitDisabled()) {
      this.toast.warning('Only an admin can change this document after an admin decision.');
      return;
    }
    this.api.reviewDocument(Number(v.id), { status: v.status }).subscribe({
      next: () => {
        this.toast.success('Updated');
        this.reloadAll();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
    });
  }
}
