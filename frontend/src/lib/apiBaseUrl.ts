const LOCALHOST_NAMES = new Set(["localhost", "127.0.0.1"]);

/**
 * Resolves the backend base URL, adapting a "localhost" configured value to
 * whatever host this page was actually loaded from. Without this, a device
 * on the LAN loading the frontend via the dev machine's IP would still try
 * to call ITS OWN localhost:5000 for the API (baked into the served bundle),
 * which doesn't exist — VITE_API_BASE_URL=http://localhost:5000 only ever
 * resolves correctly on the machine actually running the backend.
 *
 * An explicitly configured non-localhost URL (e.g. a deployed API) is
 * always used as-is.
 */
export function resolveApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL;

  try {
    const configuredUrl = new URL(configured);
    const pageHostname = window.location.hostname;

    if (LOCALHOST_NAMES.has(configuredUrl.hostname) && !LOCALHOST_NAMES.has(pageHostname)) {
      configuredUrl.hostname = pageHostname;
      return configuredUrl.toString().replace(/\/$/, "");
    }
  } catch {
    // Not a parseable absolute URL — fall through and use it as configured.
  }

  return configured;
}
