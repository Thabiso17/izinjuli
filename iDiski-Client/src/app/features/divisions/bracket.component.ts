import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatchResultDto, knockoutRoundName, shootoutSuffix } from '../../core/models';

/**
 * A knockout bracket, drawn as rounds across the page.
 *
 * The whole competition is created before anyone plays, so most of what this renders is
 * fixtures with nobody in them yet — that is the point rather than a gap. A slot waiting on
 * the round before it says so, and fills in as results arrive.
 */
@Component({
  selector: 'app-bracket',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (rounds().length === 0) {
      <p class="text-muted mb-0">This competition has no bracket yet.</p>
    } @else {
      <!-- Scrolls sideways rather than squeezing: a bracket of sixteen will not fit a phone,
           and shrinking it to fit makes it unreadable instead of merely wide. -->
      <div class="bracket" data-testid="bracket">
        @for (round of rounds(); track round.size) {
          <div class="bracket-round" data-testid="bracket-round">
            <h6 class="bracket-round-name">{{ round.name }}</h6>

            @for (tie of round.ties; track tie.id) {
              <div class="tie" data-testid="bracket-tie">
                <div class="side" [class.through]="wonBy(tie) === 'home'">
                  <span class="team">{{ tie.homeTeamName || 'To be decided' }}</span>
                  <span class="score">{{ scoreOf(tie, 'home') }}</span>
                </div>
                <div class="side" [class.through]="wonBy(tie) === 'away'">
                  <span class="team">{{ tie.awayTeamName || 'To be decided' }}</span>
                  <span class="score">{{ scoreOf(tie, 'away') }}</span>
                </div>

                @if (shootout(tie); as pens) {
                  <div class="pens" data-testid="bracket-shootout">{{ pens }}</div>
                }
              </div>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      .bracket {
        display: flex;
        gap: 1.5rem;
        overflow-x: auto;
        padding-bottom: 0.5rem;
      }

      .bracket-round {
        display: flex;
        flex-direction: column;
        justify-content: space-around;
        gap: 1rem;
        min-width: 13rem;
      }

      .bracket-round-name {
        text-align: center;
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #6c757d;
        margin-bottom: 0.25rem;
      }

      .tie {
        border: 1px solid #dee2e6;
        border-radius: 0.5rem;
        overflow: hidden;
        background: #fff;
      }

      .side {
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        padding: 0.4rem 0.6rem;
        font-size: 0.9rem;
      }

      .side + .side {
        border-top: 1px solid #f1f3f5;
      }

      .side.through {
        font-weight: 600;
        background: #f1f8f4;
      }

      .team {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .score {
        color: #6c757d;
        font-variant-numeric: tabular-nums;
      }

      .side.through .score {
        color: inherit;
      }

      .pens {
        padding: 0.25rem 0.6rem;
        font-size: 0.75rem;
        color: #6c757d;
        background: #f8f9fa;
        border-top: 1px solid #f1f3f5;
      }
    `,
  ],
})
export class BracketComponent {
  matches = input.required<MatchResultDto[]>();

  /**
   * Rounds largest first, so the bracket reads left to right towards the final. The round
   * size is how many teams were left at that point, which is also what the round is called.
   */
  rounds = computed(() => {
    const knockout = this.matches().filter(
      (m) => m.stage === 'Knockout' && m.knockoutRoundSize,
    );

    const bySize = new Map<number, MatchResultDto[]>();

    for (const match of knockout) {
      const size = match.knockoutRoundSize!;
      bySize.set(size, [...(bySize.get(size) ?? []), match]);
    }

    return [...bySize.entries()]
      .sort(([a], [b]) => b - a)
      .map(([size, ties]) => ({
        size,
        name: knockoutRoundName(size) ?? `Round of ${size}`,
        ties: [...ties].sort(
          (a, b) => new Date(a.matchDate).getTime() - new Date(b.matchDate).getTime(),
        ),
      }));
  });

  shootout(tie: MatchResultDto): string | null {
    return shootoutSuffix(tie.homePenalties, tie.awayPenalties);
  }

  /** A blank rather than a nought, so a tie nobody has played does not read as goalless. */
  scoreOf(tie: MatchResultDto, side: 'home' | 'away'): string {
    if (tie.status !== 'Completed') return '';

    return String(side === 'home' ? tie.homeScore : tie.awayScore);
  }

  /**
   * Which side went through, or null while the tie is unplayed or still level. Penalties
   * decide it when ninety minutes did not — a side can lose on the day and still advance.
   */
  wonBy(tie: MatchResultDto): 'home' | 'away' | null {
    if (tie.status !== 'Completed') return null;
    if (tie.homeScore !== tie.awayScore) return tie.homeScore > tie.awayScore ? 'home' : 'away';

    const home = tie.homePenalties;
    const away = tie.awayPenalties;

    if (home === null || away === null || home === undefined || away === undefined) return null;
    if (home === away) return null;

    return home > away ? 'home' : 'away';
  }
}
