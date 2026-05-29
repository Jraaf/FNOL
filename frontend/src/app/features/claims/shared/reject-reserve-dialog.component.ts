import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-reject-reserve-dialog',
  standalone: true,
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Reject reserve</h2>
    <mat-dialog-content>
      <p>Provide a reason — it will be recorded in the immutable audit log.</p>
      <mat-form-field appearance="outline" class="full">
        <mat-label>Rejection reason</mat-label>
        <textarea matInput rows="3" [(ngModel)]="reason" maxlength="500" required></textarea>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(null)">Cancel</button>
      <button mat-flat-button color="warn" [disabled]="!reason().trim()" (click)="ref.close(reason().trim())">
        Reject
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.full { width: 100%; min-width: 320px; }`]
})
export class RejectReserveDialogComponent {
  readonly ref = inject(MatDialogRef<RejectReserveDialogComponent, string | null>);
  readonly reason = signal('');
}
