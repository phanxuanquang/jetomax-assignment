import { ImageOff } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { useSignedImageUrl } from "./useSignedImageUrl";

export function MessageImage({ imageUrl, caption }: { imageUrl: string; caption: string | null }) {
  const { url, error } = useSignedImageUrl(imageUrl);

  return (
    <figure className="flex max-w-xs flex-col gap-1.5">
      {error ? (
        <div className="bg-muted flex h-40 w-full items-center justify-center rounded-lg">
          <ImageOff className="text-muted-foreground size-6" aria-label={error} />
        </div>
      ) : url ? (
        <img src={url} alt={caption ?? "Sent image"} className="rounded-lg" loading="lazy" />
      ) : (
        <Skeleton className="h-40 w-full rounded-lg" />
      )}
    </figure>
  );
}
