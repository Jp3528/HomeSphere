import { apiClient } from './api-client';
import {
  RoomSceneListItem,
  RoomSceneDetail,
  CreateRoomScenePayload,
  UpdateRoomScenePayload,
  CreateHotspotPayload,
  UpdateHotspotPayload,
  SceneHotspot,
} from '@/types/scenes';

export const scenesApi = {
  // Public Endpoints
  getPublicScenes: async (onlyActive: boolean = true): Promise<RoomSceneListItem[]> => {
    return apiClient.get<RoomSceneListItem[]>('/api/scenes', {
      params: { onlyActive },
    });
  },

  getPublicSceneBySlug: async (slug: string): Promise<RoomSceneDetail> => {
    return apiClient.get<RoomSceneDetail>(`/api/scenes/${slug}`);
  },

  // Admin Endpoints
  getAdminScenes: async (onlyActive: boolean = false): Promise<RoomSceneListItem[]> => {
    return apiClient.get<RoomSceneListItem[]>('/api/admin/scenes', {
      params: { onlyActive },
    });
  },

  getAdminSceneById: async (id: string): Promise<RoomSceneDetail> => {
    return apiClient.get<RoomSceneDetail>(`/api/admin/scenes/${id}`);
  },

  createScene: async (payload: CreateRoomScenePayload): Promise<RoomSceneDetail> => {
    return apiClient.post<RoomSceneDetail>('/api/admin/scenes', payload);
  },

  updateScene: async (id: string, payload: UpdateRoomScenePayload): Promise<RoomSceneDetail> => {
    return apiClient.put<RoomSceneDetail>(`/api/admin/scenes/${id}`, payload);
  },

  deleteScene: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/admin/scenes/${id}`);
  },

  addHotspot: async (sceneId: string, payload: CreateHotspotPayload): Promise<SceneHotspot> => {
    return apiClient.post<SceneHotspot>(`/api/admin/scenes/${sceneId}/hotspots`, payload);
  },

  updateHotspot: async (
    sceneId: string,
    hotspotId: string,
    payload: UpdateHotspotPayload
  ): Promise<SceneHotspot> => {
    return apiClient.put<SceneHotspot>(
      `/api/admin/scenes/${sceneId}/hotspots/${hotspotId}`,
      payload
    );
  },

  deleteHotspot: async (sceneId: string, hotspotId: string): Promise<void> => {
    await apiClient.delete(`/api/admin/scenes/${sceneId}/hotspots/${hotspotId}`);
  },
};
