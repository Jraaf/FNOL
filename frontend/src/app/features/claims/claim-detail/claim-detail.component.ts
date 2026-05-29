import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ClaimStatusDescriptor, ClaimsApiService } from '../../../core/api/claims-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  ClaimAuditEntryDto, ClaimDetailDto, ClaimStatus, DocumentLinkDto, DocumentType,
  ReserveApprovalStatus, ReserveComponentDto, ReserveComponentType
} from '../../../core/models/claim.models';
import { ConfirmDialogComponent, ConfirmDialogData } from '../shared/confirm-dialog.component';
import { RejectReserveDialogComponent } from '../shared/reject-reserve-dialog.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatCardModule, MatTabsModule, MatTableModule, MatChipsModule, MatButtonModule,
    MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatMenuModule,
    MatProgressSpinnerModule, MatDialogModule,
    StatusBadgeComponent
  ],
  templateUrl: './claim-detail.component.html',
  styleUrl: './claim-detail.component.scss'
})
export class ClaimDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ClaimsApiService);
  private readonly snack = inject(MatSnackBar);
  protected readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  justCreatedNumber = signal<string | null>(null);
  justCreatedOutsidePolicy = signal(false);

  readonly reserveTypes = [
    { label: 'Indemnity', value: ReserveComponentType.IndemnityReserve },
    { label: 'Expense', value: ReserveComponentType.ExpenseReserve },
    { label: 'Recovery', value: ReserveComponentType.RecoveryReserve },
    { label: 'Litigation', value: ReserveComponentType.LitigationReserve }
  ];

  readonly documentTypes = [
    { label: 'Police Report', value: DocumentType.PoliceReport },
    { label: 'Photo', value: DocumentType.Photo },
    { label: 'Medical Record', value: DocumentType.MedicalRecord },
    { label: 'Invoice', value: DocumentType.Invoice },
    { label: 'Correspondence', value: DocumentType.Correspondence },
    { label: 'Other', value: DocumentType.Other }
  ];

  claim = signal<ClaimDetailDto | null>(null);
  audit = signal<ClaimAuditEntryDto[]>([]);
  documents = signal<DocumentLinkDto[]>([]);
  statusDescriptors = signal<ClaimStatusDescriptor[]>([]);
  uploading = signal(false);

  newReserveType = ReserveComponentType.IndemnityReserve;
  newReserveAmount = 0;
  uploadDocumentType: DocumentType = DocumentType.Other;

  readonly allowedTransitions = computed(() => {
    const c = this.claim();
    if (!c) return [];
    return this.statusDescriptors().find(s => s.status === c.status)?.allowedTransitions ?? [];
  });

  ngOnInit(): void {
    this.api.getStatuses().subscribe(s => this.statusDescriptors.set(s));

    // Read navigation state set by the FNOL submit so we can show a "just created" banner.
    const nav = this.router.getCurrentNavigation();
    const state = (nav?.extras.state ?? history.state) as
      { justCreated?: string; outsidePolicy?: boolean } | undefined;
    if (state?.justCreated) {
      this.justCreatedNumber.set(state.justCreated);
      this.justCreatedOutsidePolicy.set(!!state.outsidePolicy);
      // Replace history state so a refresh doesn't show the banner again.
      history.replaceState({}, '');
    }

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) this.reload(id);
    });
  }

  dismissJustCreated(): void {
    this.justCreatedNumber.set(null);
    this.justCreatedOutsidePolicy.set(false);
  }

  reload(id: string): void {
    this.api.getClaim(id).subscribe(c => this.claim.set(c));
    this.api.getAudit(id).subscribe(a => this.audit.set(a));
    this.api.listDocuments(id).subscribe(d => this.documents.set(d));
  }

  canApproveReserve(r: ReserveComponentDto): boolean {
    if (r.approvalStatus === ReserveApprovalStatus.PendingManagerApproval) return this.auth.hasRole('Manager');
    if (r.approvalStatus === ReserveApprovalStatus.PendingSupervisorApproval)
      return this.auth.hasAnyRole(['Supervisor', 'Manager']);
    return false;
  }
  canRejectReserve(r: ReserveComponentDto): boolean { return this.canApproveReserve(r); }

  openReserve(): void {
    const c = this.claim();
    if (!c) return;
    if (this.newReserveAmount <= 0) return;
    this.api.openReserve(c.id, this.newReserveType, this.newReserveAmount).subscribe(() => {
      this.snack.open('Reserve opened.', 'OK', { duration: 3000 });
      this.newReserveAmount = 0;
      this.reload(c.id);
    });
  }

  approveReserve(r: ReserveComponentDto): void {
    const c = this.claim(); if (!c) return;
    this.api.approveReserve(c.id, r.id).subscribe(() => {
      this.snack.open('Reserve approved. GL posting enqueued.', 'OK', { duration: 3000 });
      this.reload(c.id);
    });
  }

  rejectReserve(r: ReserveComponentDto): void {
    const c = this.claim(); if (!c) return;
    this.dialog.open(RejectReserveDialogComponent, { width: '420px' })
      .afterClosed()
      .subscribe(reason => {
        if (!reason) return;
        this.api.rejectReserve(c.id, r.id, reason).subscribe(() => {
          this.snack.open('Reserve rejected.', 'OK', { duration: 3000 });
          this.reload(c.id);
        });
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const c = this.claim(); if (!c) return;
    this.uploading.set(true);
    this.api.uploadDocument(c.id, file, this.uploadDocumentType).subscribe({
      next: () => {
        this.uploading.set(false);
        input.value = '';
        this.snack.open(`Uploaded ${file.name}`, 'OK', { duration: 3000 });
        this.reload(c.id);
      },
      error: () => { this.uploading.set(false); input.value = ''; }
    });
  }

  transition(toStatus: ClaimStatus): void {
    const c = this.claim(); if (!c) return;
    const targetName = this.statusName(toStatus);
    const currentName = this.statusName(c.status);
    const data: ConfirmDialogData = {
      title: `Transition to ${targetName}?`,
      message: `Claim ${c.claimNumber} is currently ${currentName}. ` +
               `This will move it to ${targetName} and write an entry to the immutable audit log.`,
      confirmLabel: `Move to ${targetName}`,
      cancelLabel: 'Cancel'
    };
    this.dialog.open(ConfirmDialogComponent, { data, width: '440px' })
      .afterClosed()
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.api.transitionStatus(c.id, toStatus).subscribe(() => {
          this.snack.open(`Status changed to ${targetName}.`, 'OK', { duration: 3000 });
          this.reload(c.id);
        });
      });
  }

  statusName = (s: ClaimStatus) => ClaimStatus[s];
  approvalStatusName = (s: ReserveApprovalStatus) => ReserveApprovalStatus[s];
  reserveTypeName = (t: ReserveComponentType) => ReserveComponentType[t];
  partyTypeName = (t: number) => ['Claimant', 'Witness', 'ThirdParty', 'Insured', 'Adjuster'][t] ?? String(t);
}
