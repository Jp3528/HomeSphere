'use client';

import { useWishlistStore } from '@/stores/wishlist-store';
import { useAuthStore } from '@/stores/auth-store';
import { Heart } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

interface WishlistHeartButtonProps {
  productId: string;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

export function WishlistHeartButton({
  productId,
  className = '',
  size = 'md',
}: WishlistHeartButtonProps) {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const { isFavorited, toggleWishlist } = useWishlistStore();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const favorited = isFavorited(productId);

  const sizeClasses = {
    sm: 'h-8 w-8 p-1.5',
    md: 'h-10 w-10 p-2',
    lg: 'h-11 w-11 p-2.5',
  };

  const iconSizes = {
    sm: 'h-4 w-4',
    md: 'h-5 w-5',
    lg: 'h-6 w-6',
  };

  const handleToggle = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      router.push(`/login?redirect=/products`);
      return;
    }

    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      await toggleWishlist(productId);
    } catch (err) {
      console.error('Error toggling wishlist:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <button
      type="button"
      onClick={handleToggle}
      aria-label={favorited ? 'Eliminar de favoritos' : 'Agregar a favoritos'}
      aria-pressed={favorited}
      disabled={isSubmitting}
      className={`relative z-10 flex items-center justify-center rounded-full transition-all duration-200 focus:outline-hidden focus:ring-2 focus:ring-amber-500 focus:ring-offset-2 ${
        favorited
          ? 'bg-rose-50 text-rose-600 shadow-xs hover:bg-rose-100 dark:bg-rose-950/60 dark:text-rose-400'
          : 'bg-white/80 text-zinc-600 shadow-xs backdrop-blur-xs hover:bg-white hover:text-rose-500 dark:bg-zinc-900/80 dark:text-zinc-400 dark:hover:bg-zinc-800 dark:hover:text-rose-400'
      } ${sizeClasses[size]} ${className}`}
    >
      <Heart
        className={`${iconSizes[size]} transition-transform duration-200 ${
          favorited ? 'fill-rose-500 text-rose-500 scale-110' : 'stroke-current'
        }`}
      />
    </button>
  );
}
