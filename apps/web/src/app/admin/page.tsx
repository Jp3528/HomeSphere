import { Metadata } from "next";
import { AdminDashboardClient } from "@/components/admin/AdminDashboardClient";

export const metadata: Metadata = {
  title: "Dashboard Comercial & Analítica | Admin HomeSphere",
  description: "Panel de control ejecutivo de ventas, pedidos y operaciones para HomeSphere.",
};

export default function AdminPage() {
  return <AdminDashboardClient />;
}

