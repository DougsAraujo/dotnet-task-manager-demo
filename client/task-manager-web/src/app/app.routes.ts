import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/task-list.component').then((m) => m.TaskListComponent),
  },
  {
    path: 'tasks/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/task-editor.component').then((m) => m.TaskEditorComponent),
    data: { mode: 'create' },
  },
  {
    path: 'tasks/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/task-editor.component').then((m) => m.TaskEditorComponent),
    data: { mode: 'edit' },
  },
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: '**', redirectTo: 'tasks' },
];
