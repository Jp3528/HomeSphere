import { Suspense } from "react";
import { Metadata } from "next";
import { AdminSceneEditorClient } from "@/components/admin/AdminSceneEditorClient";
import { Loader2 } from "lucide-react";

interface EditorPageProps {
  params: Promise<{ id: string }>;
}

export const metadata: Metadata = {
  title: "Editor Visual de Ambientes | Admin HomeSphere",
  description: "Editor interactivo WYSIWYG de ambientes y coordenadas de hotspots para HomeSphere.",
};

async function EditorWrapper({ params }: EditorPageProps) {
  const { id } = await params;
  return <AdminSceneEditorClient id={id} />;
}

export default function AdminSceneEditorPage({ params }: EditorPageProps) {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-zinc-950 flex flex-col items-center justify-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-amber-500" />
          <p className="text-xs text-zinc-400 font-medium">Iniciando editor visual...</p>
        </div>
      }
    >
      <EditorWrapper params={params} />
    </Suspense>
  );
}
