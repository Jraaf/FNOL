import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CauseCode,
  ClaimAuditEntryDto,
  ClaimDetailDto,
  ClaimDocumentDto,
  ClaimStatus,
  ClaimSummary,
  CreateClaimPayload,
  CreateClaimResult,
  DocumentLinkDto,
  DocumentType,
  PagedResult,
  PolicyCoverage,
  PolicySummary,
  ReserveComponentDto,
  ReserveComponentType
} from '../models/claim.models';

export interface ListClaimsFilters {
  status?: ClaimStatus;
  lossDateFrom?: string;
  lossDateTo?: string;
  assignedHandlerUserId?: string;
  causeOfLossCode?: string;
  page?: number;
  pageSize?: number;
}

export interface ClaimStatusDescriptor {
  status: ClaimStatus;
  name: string;
  allowedTransitions: { from: ClaimStatus; to: ClaimStatus; requiredPermission?: string }[];
}

@Injectable({ providedIn: 'root' })
export class ClaimsApiService {
  private readonly http = inject(HttpClient);
  // Relative base: the browser resolves it against whatever origin served the SPA.
  // In production the Angular bundle is served same-origin from the API container's
  // wwwroot, so '/api' always points at the right host — no per-deployment/revision
  // URL to configure. For local `ng serve` (port 4200), proxy.conf.json forwards
  // '/api' to the API on http://localhost:5000.
  private readonly base = '/api';

  listClaims(filters: ListClaimsFilters): Observable<PagedResult<ClaimSummary>> {
    let params = new HttpParams();
    Object.entries(filters).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    });
    return this.http.get<PagedResult<ClaimSummary>>(`${this.base}/claims`, { params });
  }

  getClaim(id: string): Observable<ClaimDetailDto> {
    return this.http.get<ClaimDetailDto>(`${this.base}/claims/${id}`);
  }

  createClaim(payload: CreateClaimPayload): Observable<CreateClaimResult> {
    return this.http.post<CreateClaimResult>(`${this.base}/claims`, payload);
  }

  transitionStatus(id: string, toStatus: ClaimStatus, reason?: string) {
    return this.http.put<{ claimId: string; status: ClaimStatus }>(`${this.base}/claims/${id}/status`,
      { toStatus, reason });
  }

  getAudit(id: string): Observable<ClaimAuditEntryDto[]> {
    return this.http.get<ClaimAuditEntryDto[]>(`${this.base}/claims/${id}/audit`);
  }

  listReserves(claimId: string): Observable<ReserveComponentDto[]> {
    return this.http.get<ReserveComponentDto[]>(`${this.base}/claims/${claimId}/reserves`);
  }

  openReserve(claimId: string, componentType: ReserveComponentType, amount: number) {
    return this.http.post<ReserveComponentDto>(`${this.base}/claims/${claimId}/reserves`,
      { componentType, amount });
  }

  adjustReserve(claimId: string, reserveId: string, newAmount: number, changeReason: string) {
    return this.http.put<ReserveComponentDto>(`${this.base}/claims/${claimId}/reserves/${reserveId}`,
      { newAmount, changeReason });
  }

  approveReserve(claimId: string, reserveId: string, comment?: string) {
    return this.http.post<ReserveComponentDto>(
      `${this.base}/claims/${claimId}/reserves/${reserveId}/approve`, { comment });
  }

  rejectReserve(claimId: string, reserveId: string, rejectionReason: string) {
    return this.http.post<ReserveComponentDto>(
      `${this.base}/claims/${claimId}/reserves/${reserveId}/reject`, { rejectionReason });
  }

  listDocuments(claimId: string): Observable<DocumentLinkDto[]> {
    return this.http.get<DocumentLinkDto[]>(`${this.base}/claims/${claimId}/documents`);
  }

  uploadDocument(claimId: string, file: File, documentType: DocumentType): Observable<ClaimDocumentDto> {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', String(documentType));
    return this.http.post<ClaimDocumentDto>(`${this.base}/claims/${claimId}/documents`, form);
  }

  searchPolicies(q: string): Observable<PolicySummary[]> {
    return this.http.get<PolicySummary[]>(`${this.base}/policies/search`,
      { params: new HttpParams().set('q', q) });
  }

  getPolicyCoverage(id: string): Observable<PolicyCoverage[]> {
    return this.http.get<PolicyCoverage[]>(`${this.base}/policies/${id}/coverage`);
  }

  getCauseCodes(perilCategory?: string): Observable<CauseCode[]> {
    let params = new HttpParams();
    if (perilCategory) params = params.set('perilCategory', perilCategory);
    return this.http.get<CauseCode[]>(`${this.base}/reference/cause-of-loss-codes`, { params });
  }

  getStatuses(): Observable<ClaimStatusDescriptor[]> {
    return this.http.get<ClaimStatusDescriptor[]>(`${this.base}/reference/claim-statuses`);
  }
}
