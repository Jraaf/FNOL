export enum ClaimStatus {
  Draft = 0,
  Open = 1,
  UnderInvestigation = 2,
  PendingPayment = 3,
  Closed = 4,
  Reopened = 5,
  Withdrawn = 6,
  SlaBreached = 7
}

export enum PartyType {
  Claimant = 0,
  Witness = 1,
  ThirdParty = 2,
  Insured = 3,
  Adjuster = 4
}

export enum ReserveComponentType {
  IndemnityReserve = 0,
  ExpenseReserve = 1,
  RecoveryReserve = 2,
  LitigationReserve = 3
}

export enum ReserveApprovalStatus {
  AutoApproved = 0,
  PendingSupervisorApproval = 1,
  PendingManagerApproval = 2,
  Approved = 3,
  Rejected = 4
}

export enum DocumentType {
  PoliceReport = 0,
  Photo = 1,
  MedicalRecord = 2,
  Invoice = 3,
  Correspondence = 4,
  Other = 5
}

export interface PolicySummary {
  policyId: string;
  policyNumber: string;
  clientName: string;
  effectiveDate: string;
  expirationDate: string;
  status: string;
}

export interface PolicyCoverage {
  coverageCode: string;
  coverageName: string;
  limitAmount: number;
  deductibleAmount: number;
}

export interface CauseCode {
  code: string;
  description: string;
  perilCategory: string;
}

export interface ClaimSummary {
  id: string;
  claimNumber: string;
  policyNumber: string;
  clientName: string;
  lossDate: string;
  status: ClaimStatus;
  assignedHandlerName?: string;
  reserveTotal: number;
  lastTouchedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ClaimPartyDto {
  id: string;
  partyType: PartyType;
  firstName: string;
  lastName: string;
  contactEmail?: string;
  contactPhone?: string;
  addressLine?: string;
}

export interface ClaimRiskObjectDto {
  id: string;
  insuredAssetType: string;
  assetReference: string;
  damageDescription?: string;
  estimatedDamageAmount?: number;
}

export interface ReserveHistoryDto {
  changeSequence: number;
  previousAmount: number;
  newAmount: number;
  changeReason: string;
  changedBy: string;
  changedAt: string;
}

export interface ReserveComponentDto {
  id: string;
  componentType: ReserveComponentType;
  currentAmount: number;
  approvalStatus: ReserveApprovalStatus;
  rejectionReason?: string;
  changeSequence: number;
  lastApprovedAt?: string;
  lastApprovedBy?: string;
  history: ReserveHistoryDto[];
}

export interface ClaimDocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  documentType: DocumentType;
  uploadedAt: string;
  uploadedBy: string;
}

export interface DocumentLinkDto extends ClaimDocumentDto {
  downloadUrl: string;
}

export interface ClaimAuditEntryDto {
  id: string;
  eventType: string;
  oldValues?: string;
  newValues?: string;
  description?: string;
  triggeredBy: string;
  occurredAt: string;
}

export interface ClaimDetailDto {
  id: string;
  claimNumber: string;
  policyNumber: string;
  clientName: string;
  policyId: string;
  lossDate: string;
  reportedAt: string;
  status: ClaimStatus;
  causeOfLossCode: string;
  lossLocation: string;
  lossDescription: string;
  assignedHandlerName?: string;
  lossDateOutsidePolicyPeriod: boolean;
  managerOverrideFlag: boolean;
  parties: ClaimPartyDto[];
  riskObjects: ClaimRiskObjectDto[];
  reserves: ReserveComponentDto[];
  documents: ClaimDocumentDto[];
}

export interface CreateClaimPayload {
  policyId: string;
  lossDate: string;
  causeOfLossCode: string;
  lossLocation: string;
  lossDescription: string;
  parties: {
    partyType: PartyType;
    firstName: string;
    lastName: string;
    contactEmail?: string;
    contactPhone?: string;
    addressLine?: string;
  }[];
  riskObjects: {
    insuredAssetType: string;
    assetReference: string;
    damageDescription?: string;
    estimatedDamageAmount?: number;
  }[];
  initialReserve?: { componentType: ReserveComponentType; amount: number } | null;
}

export interface CreateClaimResult {
  claimId: string;
  claimNumber: string;
  status: ClaimStatus;
  lossDateOutsidePolicyPeriod: boolean;
}
