export interface SceneHotspot {
  id: string;
  roomSceneId: string;
  productId: string;
  productName: string;
  productSlug: string;
  productBasePrice: number;
  productPrimaryImageUrl?: string;
  productVariantId?: string;
  productVariantName?: string;
  productVariantPrice?: number;
  xCoordinate: number;
  yCoordinate: number;
  title?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface RoomSceneListItem {
  id: string;
  title: string;
  slug: string;
  description: string;
  imageUrl: string;
  hotspotCount: number;
  displayOrder: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface RoomSceneDetail {
  id: string;
  title: string;
  slug: string;
  description: string;
  imageUrl: string;
  displayOrder: number;
  isActive: boolean;
  hotspots: SceneHotspot[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}
