import { Injectable, Type } from '@angular/core';
import { FixturesSectionComponent } from '../sections/fixtures-section.component';
import { SponsorsSectionComponent } from '../sections/sponsors-section.component';
import { VideosSectionComponent } from '../sections/videos-section.component';
import { ArticlesSectionComponent } from '../sections/articles-section.component';
import { StandingsSectionComponent } from '../sections/standings-section.component';

export interface ComponentRegistryEntry {
  component: Type<any>;
  defaultZone: 'main' | 'sidebar';
}

@Injectable({ providedIn: 'root' })
export class ComponentRegistryService {
  private readonly registry = new Map<string, ComponentRegistryEntry>([
    ['Fixtures', { component: FixturesSectionComponent, defaultZone: 'main' }],
    ['Sponsors', { component: SponsorsSectionComponent, defaultZone: 'sidebar' }],
    ['Videos', { component: VideosSectionComponent, defaultZone: 'main' }],
    ['Articles', { component: ArticlesSectionComponent, defaultZone: 'main' }],
    ['Standings', { component: StandingsSectionComponent, defaultZone: 'sidebar' }],
  ]);

  getComponent(componentName: string): ComponentRegistryEntry | undefined {
    return this.registry.get(componentName);
  }

  getAllComponentNames(): string[] {
    return Array.from(this.registry.keys());
  }
}
