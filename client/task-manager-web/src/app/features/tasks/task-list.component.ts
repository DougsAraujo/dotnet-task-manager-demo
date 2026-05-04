import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskItemStatus, type TaskItemResponse } from '../../core/task.models';
import { TaskService } from '../../core/task.service';
import { LocaleService } from '../../core/locale.service';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-task-list',
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss',
})
export class TaskListComponent {
  private readonly tasksApi = inject(TaskService);
  protected readonly locale = inject(LocaleService);

  protected readonly tasks = signal<TaskItemResponse[]>([]);
  protected readonly error = signal('');
  protected readonly busy = signal(true);

  constructor() {
    this.reload();
  }

  protected statusLabel(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.Pending:
        return this.locale.t('tasks.list.statusPending');
      case TaskItemStatus.InProgress:
        return this.locale.t('tasks.list.statusInProgress');
      case TaskItemStatus.Completed:
        return this.locale.t('tasks.list.statusCompleted');
      default:
        return '';
    }
  }

  reload(): void {
    this.busy.set(true);
    this.error.set('');
    this.tasksApi.list().subscribe({
      next: (t) => {
        this.tasks.set(t);
        this.busy.set(false);
      },
      error: () => {
        this.error.set(this.locale.t('tasks.list.loadError'));
        this.busy.set(false);
      },
    });
  }

  remove(task: TaskItemResponse): void {
    const msg = this.locale.t('tasks.list.confirmDelete', { title: task.title });
    if (!window.confirm(msg)) {
      return;
    }

    this.tasksApi.delete(task.id).subscribe({
      next: () => this.reload(),
      error: () => this.error.set(this.locale.t('tasks.list.deleteError')),
    });
  }
}
