import { supabase } from "./client";
import { generateUuid } from "@/lib/uuid";

const IMAGES_BUCKET = "images";
const SIGNED_URL_EXPIRY_SECONDS = 60 * 60; // 1 hour, refreshed lazily on render

/**
 * Uploads an image directly to Supabase Storage and returns a bare
 * "bucket/path" string (never a full URL). The backend's own storage code
 * parses this exact shape, and storing the path instead of a signed URL
 * means old messages never break when a previously-issued signed URL expires.
 */
export async function uploadConversationImage(
  conversationId: string,
  file: File,
): Promise<string> {
  const extension = file.name.includes(".") ? file.name.split(".").pop() : undefined;
  const fileName = `${generateUuid()}${extension ? `.${extension}` : ""}`;
  const objectPath = `${conversationId}/${fileName}`;

  const { error } = await supabase.storage.from(IMAGES_BUCKET).upload(objectPath, file, {
    contentType: file.type || undefined,
  });

  if (error) {
    throw error;
  }

  return `${IMAGES_BUCKET}/${objectPath}`;
}

/**
 * Extracts the object path (relative to the "images" bucket) from whatever
 * shape a message's imageUrl field holds: a bare "images/path", a
 * "/object/public/images/path", or a "/object/sign/images/path?token=..." URL.
 */
function extractObjectPath(imageUrl: string): string | null {
  const bucketMarker = `${IMAGES_BUCKET}/`;

  if (!imageUrl.startsWith("http")) {
    const bareIndex = imageUrl.indexOf(bucketMarker);
    return bareIndex === -1 ? null : imageUrl.slice(bareIndex + bucketMarker.length);
  }

  const objectMarker = `/object/`;
  const objectIndex = imageUrl.indexOf(objectMarker);
  if (objectIndex === -1) {
    return null;
  }

  const afterObject = imageUrl.slice(objectIndex + objectMarker.length); // "sign/images/..." or "public/images/..."
  const segments = afterObject.split("/");
  const bucketIndex = segments.indexOf(IMAGES_BUCKET);
  if (bucketIndex === -1) {
    return null;
  }

  const path = segments.slice(bucketIndex + 1).join("/");
  const withoutQuery = path.split("?")[0];
  return withoutQuery || null;
}

/**
 * Resolves a message's stored imageUrl value to a short-lived signed URL
 * suitable for an <img> src. Callers should cache the result themselves
 * (see useSignedImageUrl) instead of calling this on every render.
 */
export async function resolveSignedImageUrl(imageUrl: string): Promise<string> {
  const objectPath = extractObjectPath(imageUrl);
  if (!objectPath) {
    throw new Error(`Could not resolve a storage path from imageUrl: ${imageUrl}`);
  }

  const { data, error } = await supabase.storage
    .from(IMAGES_BUCKET)
    .createSignedUrl(objectPath, SIGNED_URL_EXPIRY_SECONDS);

  if (error || !data) {
    throw error ?? new Error("Supabase returned no signed URL");
  }

  return data.signedUrl;
}

export const IMAGE_SIGNED_URL_TTL_MS = SIGNED_URL_EXPIRY_SECONDS * 1000;
