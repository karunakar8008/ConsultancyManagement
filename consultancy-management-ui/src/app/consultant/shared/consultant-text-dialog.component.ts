import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ConsultantTextDialogData {
  title: string;
  label: string;
  value: string;
  /** Textarea rows; default 4 */
  rows?: number;
}

@Component({
  selector: 'app-consultant-text-dialog',
  standalone: true,
  imports: [MatDialogModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  styles: [
    `
      .full {
        width: 100%;
      }
      mat-dialog-content {
        min-width: min(420px, 88vw);
      }
    `
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full">
        <mat-label>{{ data.label }}</mat-label>
        <textarea matInput [rows]="data.rows ?? 4" [formControl]="ctrl"></textarea>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancel</button>
      <button mat-flat-button color="primary" type="button" (click)="save()">Save</button>
    </mat-dialog-actions>
  `
})
export class ConsultantTextDialogComponent {
  readonly data = inject<ConsultantTextDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ConsultantTextDialogComponent, string | undefined>);

  readonly ctrl = new FormControl(this.data.value ?? '', { nonNullable: true });

  cancel(): void {
    this.ref.close(undefined);
  }

  save(): void {
    this.ref.close(this.ctrl.value);
  }
}
