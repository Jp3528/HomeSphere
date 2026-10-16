import { Suspense } from 'react';
import OrderDetailClient from '@/components/orders/OrderDetailClient';
import { RefreshCw } from 'lucide-react';

interface OrderPageProps {
  params: Promise<{ publicId: string }>;
}

async function OrderDetailWrapper({ params }: OrderPageProps) {
  const { publicId } = await params;
  return <OrderDetailClient publicId={publicId} />;
}

export default function OrderDetailPage({ params }: OrderPageProps) {
  return (
    <Suspense
      fallback={
        <div className="min-h-[60vh] flex items-center justify-center">
          <div className="text-center">
            <RefreshCw className="w-8 h-8 text-amber-700 animate-spin mx-auto mb-3" />
            <p className="text-sm font-medium text-stone-600">Cargando pedido...</p>
          </div>
        </div>
      }
    >
      <OrderDetailWrapper params={params} />
    </Suspense>
  );
}
