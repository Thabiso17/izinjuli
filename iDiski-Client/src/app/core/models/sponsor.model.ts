export enum SponsorTier {
  Title = 0,
  Gold = 1,
  Silver = 2,
  Bronze = 3
}

export enum AdPlacement {
  Header = 0,
  Sidebar = 1,
  Footer = 2,
  MatchDay = 3,
  Homepage = 4,
  NewsPage = 5
}

export interface SponsorDto {
  id: string;
  name: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  adImageUrl: string | null;
  adLinkUrl: string | null;
  tier: SponsorTier;
  placement: AdPlacement;
  isActive: boolean;
  contractStart: string | null;
  contractEnd: string | null;
  displayOrder: number;
}
