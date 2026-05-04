import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { ManagementService } from '../../core/services/management.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-onboarding',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule
  ],
  templateUrl: './management-onboarding.component.html',
  styleUrl: '../../consultant/daily-activities/consultant-shared.scss'
})
export class ManagementOnboardingComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  cols = ['consultant', 'task', 'status', 'due', 'completed'];
  rows: Record<string, unknown>[] = [];
  consultantOptions: { id: number; label: string }[] = [];

  readonly taskTypes = [
    {
      key: 'documents',
      label: 'Documents',
      taskName: 'Documents',
      description: 'Upload required documents from Consultant → Documents in your login.'
    },
    {
      key: 'timesheet',
      label: 'Timesheet',
      taskName: 'Timesheet',
      description: 'Submit your timesheet as instructed by management.'
    }
  ];

  form = this.fb.nonNullable.group({
    consultantId: [0, [Validators.required, Validators.min(1)]],
    taskType: ['', Validators.required],
    description: [''],
    dueDate: [''],
    status: ['Pending']
  });

  ngOnInit(): void {
    this.reload();
    this.api.consultants().subscribe({
      next: (list) => {
        const arr = list as { id?: number; firstName?: string; lastName?: string; userEmployeeId?: string }[];
        this.consultantOptions = arr.map((c) => ({
          id: Number(c.id),
          label: `${c.firstName ?? ''} ${c.lastName ?? ''} (${c.userEmployeeId ?? ''})`.trim()
        }));
      },
      error: () => {}
    });

    this.form.controls.taskType.valueChanges.subscribe((key) => {
      const t = this.taskTypes.find((x) => x.key === key);
      if (t) this.form.patchValue({ description: t.description }, { emitEvent: false });
    });
  }

  reload(): void {
    this.api.onboarding().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load')
    });
  }

  save(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const v = this.form.getRawValue();
    const t = this.taskTypes.find((x) => x.key === v.taskType);
    const taskName = t?.taskName ?? v.taskType;
    const description = (v.description || t?.description || '').trim();
    this.api
      .createTask({
        consultantId: Number(v.consultantId),
        taskName,
        description: description || null,
        dueDate: v.dueDate ? new Date(v.dueDate).toISOString() : null,
        status: v.status
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Task created');
          this.form.reset({
            consultantId: 0,
            taskType: '',
            description: '',
            dueDate: '',
            status: 'Pending'
          });
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
