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
  isAvailable?: boolean;
  unavailableReason?: string;
}

export interface RoomSceneListItem {
  id: string;
  title: string;
  slug: string;
  description: string;
  imageUrl: string;
  roomType: string;
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
  roomType: string;
  displayOrder: number;
  isActive: boolean;
  hotspots: SceneHotspot[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateRoomScenePayload {
  title: string;
  slug: string;
  description: string;
  imageUrl: string;
  roomType?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateRoomScenePayload {
  title: string;
  slug: string;
  description: string;
  imageUrl: string;
  roomType?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface CreateHotspotPayload {
  productId: string;
  productVariantId?: string;
  xCoordinate: number;
  yCoordinate: number;
  title?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateHotspotPayload {
  productId: string;
  productVariantId?: string;
  xCoordinate: number;
  yCoordinate: number;
  title?: string;
  displayOrder?: number;
  isActive?: boolean;
}

