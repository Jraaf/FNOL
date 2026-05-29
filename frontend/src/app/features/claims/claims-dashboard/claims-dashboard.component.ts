import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { ClaimsApiService, ListClaimsFilters } from '../../../core/api/claims-api.service';
import { CauseCode, ClaimStatus, ClaimSummary, PagedResult } from '../../../core/models/claim.models';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-claims-dashboard',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatTableModule, MatPaginatorModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule,
    StatusBadgeComponent
  ],
  templateUrl: './claims-dashboard.component.html',
  styleUrl: './claims-dashboard.component.scss'
})
export class ClaimsDashboardComponent implements OnInit {
  private readonly api = inject(ClaimsApiService);
  private readonly router = inject(Router);

  readonly statuses = Object.entries(ClaimStatus)
    .filter(([, v]) => typeof v === 'number')
    .map(([k, v]) => ({ label: k, value: v as number }));

  readonly displayedColumns = [
    'claimNumber', 'policyNumber', 'clientName', 'lossDate', 'status', 'reserveTotal'
  ];

  filters = signal<ListClaimsFilters>({ page: 1, pageSize: 10 });
  page = signal<PagedResult<ClaimSummary> | null>(null);
  loading = signal(false);
  causeCodes = signal<CauseCode[]>([]);

  ngOnInit(): void {
    this.api.getCauseCodes().subscribe(codes => this.causeCodes.set(codes));
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.listClaims(this.filters()).subscribe({
      next: result => { this.page.set(result); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  applyFilter<K extends keyof ListClaimsFilters>(key: K, value: ListClaimsFilters[K]): void {
    this.filters.update(f => ({ ...f, [key]: value, page: 1 }));
    this.reload();
  }

  resetFilters(): void {
    this.filters.set({ page: 1, pageSize: 10 });
    this.reload();
  }

  onPage(e: PageEvent): void {
    this.filters.update(f => ({ ...f, page: e.pageIndex + 1, pageSize: e.pageSize }));
    this.reload();
  }

  openClaim(row: ClaimSummary): void {
    this.router.navigate(['/claims', row.id]);
  }

  newClaim(): void {
    this.router.navigate(['/claims/new']);
  }
}
