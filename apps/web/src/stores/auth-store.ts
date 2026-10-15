import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { apiClient, setAuthToken } from '@/lib/api-client';

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserDto;
}

interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAtUtc: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName: string, lastName: string) => Promise<void>;
  logout: () => Promise<void>;
  fetchProfile: () => Promise<void>;
  clearError: () => void;
  hasRole: (role: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      expiresAtUtc: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      clearError: () => set({ error: null }),

      hasRole: (role: string) => {
        const { user } = get();
        return user?.roles.includes(role) ?? false;
      },

      login: async (email: string, password: string) => {
        set({ isLoading: true, error: null });
        try {
          const res = await apiClient.post<AuthResponse>('/api/auth/login', { email, password });
          setAuthToken(res.accessToken);
          set({
            user: res.user,
            accessToken: res.accessToken,
            refreshToken: res.refreshToken,
            expiresAtUtc: res.expiresAtUtc,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (err: unknown) {
          const message = err instanceof Error ? err.message : 'Error al iniciar sesión';
          set({ error: message, isLoading: false });
          throw err;
        }
      },

      register: async (email: string, password: string, firstName: string, lastName: string) => {
        set({ isLoading: true, error: null });
        try {
          const res = await apiClient.post<AuthResponse>('/api/auth/register', {
            email,
            password,
            firstName,
            lastName,
          });
          setAuthToken(res.accessToken);
          set({
            user: res.user,
            accessToken: res.accessToken,
            refreshToken: res.refreshToken,
            expiresAtUtc: res.expiresAtUtc,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (err: unknown) {
          const message = err instanceof Error ? err.message : 'Error al registrarse';
          set({ error: message, isLoading: false });
          throw err;
        }
      },

      logout: async () => {
        const { refreshToken } = get();
        if (refreshToken) {
          try {
            await apiClient.post('/api/auth/revoke', { refreshToken });
          } catch {
            // Ignore revocation network failures on logout
          }
        }
        setAuthToken(null);
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          expiresAtUtc: null,
          isAuthenticated: false,
          error: null,
        });
      },

      fetchProfile: async () => {
        const { accessToken } = get();
        if (!accessToken) return;
        setAuthToken(accessToken);
        try {
          const profile = await apiClient.get<UserDto>('/api/me');
          set({ user: profile, isAuthenticated: true });
        } catch {
          get().logout();
        }
      },
    }),
    {
      name: 'homesphere-auth-storage',
      storage: createJSONStorage(() => localStorage),
      onRehydrateStorage: () => (state) => {
        if (state?.accessToken) {
          setAuthToken(state.accessToken);
        }
      },
    }
  )
);
