import { Component, computed, input } from '@angular/core';
import { ClaimStatus } from '../../../core/models/claim.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="status-badge {{ cssClass() }}">{{ label() }}</span>`
})
export class StatusBadgeComponent {
  status = input.required<ClaimStatus>();
  label = computed(() => ClaimStatus[this.status()]);
  cssClass = computed(() => `status-${ClaimStatus[this.status()].toLowerCase()}`);
}
