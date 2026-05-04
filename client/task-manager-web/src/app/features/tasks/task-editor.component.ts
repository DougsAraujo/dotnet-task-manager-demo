import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TaskItemStatus } from '../../core/task.models';
import { TaskService } from '../../core/task.service';
import { LocaleService, showControlErrors } from '../../core/locale.service';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-task-editor',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './task-editor.component.html',
  styleUrl: './task-editor.component.scss',
})
export class TaskEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly tasksApi = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly locale = inject(LocaleService);

  protected readonly statusOptions = [
    { value: TaskItemStatus.Pending, labelKey: 'tasks.list.statusPending' },
    { value: TaskItemStatus.InProgress, labelKey: 'tasks.list.statusInProgress' },
    { value: TaskItemStatus.Completed, labelKey: 'tasks.list.statusCompleted' },
  ];

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(4000)]],
    status: [TaskItemStatus.Pending, Validators.required],
    dueDateDate: [''],
    dueDateTime: [''],
  });

  protected submitted = false;
  protected apiError = '';
  protected loadError = '';
  protected busy = false;
  protected taskId: string | null = null;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.apiError = '';
    });
    this.form.controls.dueDateDate.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((v) => {
      if (!v) {
        this.form.controls.dueDateTime.setValue('', { emitEvent: false });
      }
    });
  }

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id');
    if (!this.taskId) {
      return;
    }

    this.busy = true;
    this.loadError = '';
    this.tasksApi.get(this.taskId).subscribe({
      next: (t) => {
        const due = splitIsoToDateTimeParts(t.dueDate);
        this.form.patchValue({
          title: t.title,
          description: t.description,
          status: t.status,
          dueDateDate: due.date,
          dueDateTime: due.time,
        });
        this.busy = false;
      },
      error: (err) => {
        this.busy = false;
        this.loadError = this.locale.parseHttpError(err, 'tasks.editor.notFound');
      },
    });
  }

  protected showField(name: 'title' | 'description'): boolean {
    return showControlErrors(this.form.get(name), this.submitted);
  }

  protected fieldMsgs(name: 'title' | 'description'): string[] {
    return this.locale.validationMessages(this.form.get(name)?.errors);
  }

  submit(): void {
    this.submitted = true;
    this.apiError = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const body = {
      title: raw.title,
      description: raw.description,
      status: Number(raw.status) as TaskItemStatus,
      dueDate: combineLocalDateTimeToIso(raw.dueDateDate, raw.dueDateTime),
    };

    this.busy = true;
    const req$ = this.taskId ? this.tasksApi.update(this.taskId, body) : this.tasksApi.create(body);

    req$.subscribe({
      next: () => void this.router.navigateByUrl('/tasks'),
      error: (err) => {
        this.busy = false;
        this.apiError = this.locale.parseHttpError(err, 'tasks.editor.saveError');
      },
      complete: () => {
        this.busy = false;
      },
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/tasks');
  }
}

function splitIsoToDateTimeParts(iso: string | null): { date: string; time: string } {
  if (!iso) {
    return { date: '', time: '' };
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return { date: '', time: '' };
  }
  const pad = (n: number) => n.toString().padStart(2, '0');
  return {
    date: `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`,
    time: `${pad(d.getHours())}:${pad(d.getMinutes())}`,
  };
}

function combineLocalDateTimeToIso(dateStr: string, timeStr: string): string | null {
  const dPart = dateStr?.trim();
  if (!dPart) {
    return null;
  }
  const tPart = timeStr?.trim() ? timeStr : '00:00';
  const d = new Date(`${dPart}T${tPart}`);
  if (Number.isNaN(d.getTime())) {
    return null;
  }
  return d.toISOString();
}
