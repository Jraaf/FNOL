import { Routes } from '@angular/router';

export const CLAIMS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./claims-dashboard/claims-dashboard.component').then(m => m.ClaimsDashboardComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./fnol-intake/fnol-intake.component').then(m => m.FnolIntakeComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./claim-detail/claim-detail.component').then(m => m.ClaimDetailComponent)
  }
];
