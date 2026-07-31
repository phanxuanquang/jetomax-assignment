function hashString(value: string): number {
  let hash = 0;
  for (let index = 0; index < value.length; index++) {
    hash = (hash * 31 + value.charCodeAt(index)) >>> 0;
  }
  return hash;
}

/** Deterministic, pleasant background color for a given seed (e.g. a username) — same person, same color, every time. */
export function avatarColorForSeed(seed: string): string {
  const hue = hashString(seed) % 360;
  return `oklch(0.6 0.16 ${hue})`;
}
