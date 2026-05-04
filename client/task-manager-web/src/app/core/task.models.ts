export enum TaskItemStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
}

export interface TaskItemResponse {
  id: string;
  userId: string;
  title: string;
  description: string;
  status: TaskItemStatus;
  dueDate: string | null;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  status: TaskItemStatus;
  dueDate: string | null;
}

export type UpdateTaskRequest = CreateTaskRequest;

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName: string;
}
