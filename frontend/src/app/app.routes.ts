import { Routes } from '@angular/router';
import { RagWorkspaceComponent } from './rag-workspace/rag-workspace.component';

export const routes: Routes = [
  { path: '', component: RagWorkspaceComponent },
  { path: '**', redirectTo: '' }
];
