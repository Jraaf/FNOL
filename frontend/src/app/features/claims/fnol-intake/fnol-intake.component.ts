import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { ClaimsApiService } from '../../../core/api/claims-api.service';
import {
  CauseCode, CreateClaimPayload, PartyType, PolicySummary, ReserveComponentType
} from '../../../core/models/claim.models';

@Component({
  selector: 'app-fnol-intake',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatStepperModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './fnol-intake.component.html',
  styleUrl: './fnol-intake.component.scss'
})
export class FnolIntakeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ClaimsApiService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly partyTypes = [
    { label: 'Claimant', value: PartyType.Claimant },
    { label: 'Witness', value: PartyType.Witness },
    { label: 'Third Party', value: PartyType.ThirdParty },
    { label: 'Insured', value: PartyType.Insured },
    { label: 'Adjuster', value: PartyType.Adjuster }
  ];
  readonly reserveTypes = [
    { label: 'Indemnity Reserve', value: ReserveComponentType.IndemnityReserve },
    { label: 'Expense Reserve', value: ReserveComponentType.ExpenseReserve },
    { label: 'Litigation Reserve', value: ReserveComponentType.LitigationReserve }
  ];

  causeCodes = signal<CauseCode[]>([]);
  policyOptions = signal<PolicySummary[]>([]);
  submitting = signal(false);

  step1!: FormGroup;
  step2!: FormGroup;
  step3!: FormGroup;

  ngOnInit(): void {
    this.api.getCauseCodes().subscribe(codes => this.causeCodes.set(codes));

    this.step1 = this.fb.group({
      policyQuery: ['', Validators.required],
      policyId: ['', Validators.required],
      lossDate: ['', Validators.required],
      causeOfLossCode: ['', Validators.required],
      lossLocation: ['', [Validators.required, Validators.maxLength(255)]],
      lossDescription: ['', Validators.required]
    });

    this.step2 = this.fb.group({
      parties: this.fb.array([
        this.buildPartyGroup(PartyType.Claimant)
      ], { validators: [hasOneClaimant] }),
      riskObjects: this.fb.array([])
    });

    this.step3 = this.fb.group({
      includeInitialReserve: [false],
      reserveType: [ReserveComponentType.IndemnityReserve],
      reserveAmount: [0, [Validators.min(0.01)]]
    });

    this.step1.controls['policyQuery'].valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(q => this.api.searchPolicies(q ?? ''))
      )
      .subscribe(results => this.policyOptions.set(results));
  }

  get parties() { return this.step2.get('parties') as FormArray<FormGroup>; }
  get riskObjects() { return this.step2.get('riskObjects') as FormArray<FormGroup>; }

  buildPartyGroup(type: PartyType = PartyType.Witness): FormGroup {
    return this.fb.group({
      partyType: [type, Validators.required],
      firstName: ['', [Validators.required, Validators.maxLength(255)]],
      lastName: ['', [Validators.required, Validators.maxLength(255)]],
      contactEmail: ['', [Validators.email]],
      contactPhone: [''],
      addressLine: ['']
    });
  }

  addParty(): void { this.parties.push(this.buildPartyGroup()); }
  removeParty(i: number): void { this.parties.removeAt(i); }

  addRiskObject(): void {
    this.riskObjects.push(this.fb.group({
      insuredAssetType: ['', Validators.required],
      assetReference: ['', Validators.required],
      damageDescription: [''],
      estimatedDamageAmount: [null]
    }));
  }
  removeRiskObject(i: number): void { this.riskObjects.removeAt(i); }

  pickPolicy(policy: PolicySummary): void {
    this.step1.patchValue({ policyId: policy.policyId, policyQuery: `${policy.policyNumber} — ${policy.clientName}` });
  }

  reserveBand(): 'auto' | 'supervisor' | 'manager' | 'invalid' {
    if (!this.step3.value.includeInitialReserve) return 'auto';
    const amt = Number(this.step3.value.reserveAmount ?? 0);
    if (amt <= 0) return 'invalid';
    if (amt <= 10_000) return 'auto';
    if (amt <= 100_000) return 'supervisor';
    return 'manager';
  }

  submit(): void {
    if (this.step1.invalid || this.step2.invalid || this.step3.invalid) return;
    const payload: CreateClaimPayload = {
      policyId: this.step1.value.policyId,
      lossDate: new Date(this.step1.value.lossDate).toISOString(),
      causeOfLossCode: this.step1.value.causeOfLossCode,
      lossLocation: this.step1.value.lossLocation,
      lossDescription: this.step1.value.lossDescription,
      parties: this.parties.controls.map(g => g.value),
      riskObjects: this.riskObjects.controls.map(g => g.value),
      initialReserve: this.step3.value.includeInitialReserve
        ? { componentType: this.step3.value.reserveType, amount: Number(this.step3.value.reserveAmount) }
        : null
    };

    this.submitting.set(true);
    this.api.createClaim(payload).subscribe({
      next: result => {
        this.submitting.set(false);
        this.snack.open(`Claim ${result.claimNumber} created.`, 'Open', { duration: 4000 })
          .onAction()
          .subscribe(() => this.router.navigate(['/claims', result.claimId]));
        this.router.navigate(['/claims', result.claimId], {
          state: { justCreated: result.claimNumber, outsidePolicy: result.lossDateOutsidePolicyPeriod }
        });
      },
      error: () => this.submitting.set(false)
    });
  }
}

function hasOneClaimant(group: { value: { partyType: PartyType }[] } | any) {
  const parties = (group as FormArray).controls.map((c: any) => c.value.partyType);
  return parties.includes(PartyType.Claimant) ? null : { noClaimant: true };
}
