import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import type { CreateTaskRequest, TaskItemResponse, UpdateTaskRequest } from './task.models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/tasks`;

  list() {
    return this.http.get<TaskItemResponse[]>(this.base);
  }

  get(id: string) {
    return this.http.get<TaskItemResponse>(`${this.base}/${id}`);
  }

  create(body: CreateTaskRequest) {
    return this.http.post<TaskItemResponse>(this.base, body);
  }

  update(id: string, body: UpdateTaskRequest) {
    return this.http.put<TaskItemResponse>(`${this.base}/${id}`, body);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
