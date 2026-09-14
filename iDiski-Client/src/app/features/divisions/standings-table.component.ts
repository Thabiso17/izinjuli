import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StandingDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';

/**
 * A league table.
 *
 * Pulled out of the division page because a group stage needs one per group: merging the
 * groups into a single table ranks teams that have never played each other, and two copies
 * of this markup would drift apart the first time a column changed.
 */
@Component({
  selector: 'app-standings-table',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    @if (rows().length === 0) {
      <p class="text-muted text-center py-3 mb-0">{{ emptyMessage() }}</p>
    } @else {
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light">
            <tr>
              <th scope="col">#</th>
              <th scope="col">Team</th>
              <th scope="col" class="text-center">P</th>
              <th scope="col" class="text-center">W</th>
              <th scope="col" class="text-center">D</th>
              <th scope="col" class="text-center">L</th>
              <th scope="col" class="text-center">GD</th>
              <th scope="col" class="text-center">Pts</th>
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track row.teamId) {
              <tr>
                <td class="text-muted">{{ row.position }}</td>
                <td>
                  <a
                    [routerLink]="['/teams', row.teamId]"
                    class="text-decoration-none text-dark d-flex align-items-center gap-2"
                  >
                    @if (getImageUrl(row.logoUrl)) {
                      <img
                        [src]="getImageUrl(row.logoUrl)"
                        [alt]="row.teamName"
                        style="width: 24px; height: 24px; object-fit: contain"
                      />
                    }
                    <span class="fw-semibold">{{ row.teamName }}</span>
                  </a>
                </td>
                <td class="text-center">{{ row.played }}</td>
                <td class="text-center">{{ row.won }}</td>
                <td class="text-center">{{ row.drawn }}</td>
                <td class="text-center">{{ row.lost }}</td>
                <td class="text-center">{{ row.goalDifference }}</td>
                <td class="text-center fw-bold">{{ row.points }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class StandingsTableComponent {
  rows = input.required<StandingDto[]>();
  emptyMessage = input('No matches played yet.');

  getImageUrl = getImageUrl;
}
