const relativeTimeFormat = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });

const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 1000 * 60 * 60 * 24 * 365],
  ["month", 1000 * 60 * 60 * 24 * 30],
  ["day", 1000 * 60 * 60 * 24],
  ["hour", 1000 * 60 * 60],
  ["minute", 1000 * 60],
];

/** "3 hours ago", "in 2 days", etc. Falls back to "just now" for anything under a minute. */
export function formatRelativeTime(isoTimestamp: string): string {
  const diffMs = new Date(isoTimestamp).getTime() - Date.now();

  for (const [unit, unitMs] of UNITS) {
    if (Math.abs(diffMs) >= unitMs) {
      return relativeTimeFormat.format(Math.round(diffMs / unitMs), unit);
    }
  }

  return "just now";
}

export function formatDateTime(isoTimestamp: string): string {
  return new Date(isoTimestamp).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

/** Just the time, e.g. "3:45 PM" — for the small label shown on every message bubble. */
export function formatShortTime(isoTimestamp: string): string {
  return new Date(isoTimestamp).toLocaleTimeString(undefined, { timeStyle: "short" });
}
