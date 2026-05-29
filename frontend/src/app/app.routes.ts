import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'claims' },
  {
    path: 'claims',
    loadChildren: () => import('./features/claims/claims.routes').then(m => m.CLAIMS_ROUTES)
  },
  { path: '**', redirectTo: 'claims' }
];
