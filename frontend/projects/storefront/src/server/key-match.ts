import { createHash, timingSafeEqual } from 'node:crypto';

/**
 * Constant-time check of a presented credential against the accepted values (ADR-04 rule 5).
 * Both sides are hashed first so that comparisons take the same time whatever the lengths, and every accepted
 * value is compared (no early exit).
 */
export function matchesAnyKey(presented: string | undefined, accepted: readonly string[]): boolean {
  const presentedDigest = digest(presented ?? '');
  let matched = false;
  for (const key of accepted) {
    const equal = timingSafeEqual(presentedDigest, digest(key));
    matched = matched || equal;
  }
  return matched && presented !== undefined && presented.length > 0;
}

function digest(value: string): Buffer {
  return createHash('sha256').update(value, 'utf8').digest();
}
