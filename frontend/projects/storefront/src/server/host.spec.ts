import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { normalizeHost } from './host';

interface HostVector {
  input: string;
  expected: string | null;
}

// Shared with the API (PlatformHost.Normalize): both implementations must agree (ADR-03 test vectors; TH-10).
// Unit tests run from the workspace root (frontend/).
const { vectors } = JSON.parse(
  readFileSync(join(process.cwd(), '..', 'contracts', 'test-vectors', 'host-normalization.json'), 'utf8'),
) as { vectors: HostVector[] };

describe('normalizeHost', () => {
  it('has shared vectors to run', () => {
    expect(vectors.length).toBeGreaterThan(0);
  });

  it.each(vectors.map((vector) => [vector.input, vector.expected] as const))('%j → %j (shared vector)', (input, expected) => {
    expect(normalizeHost(input)).toBe(expected);
  });

  it('rejects a missing host', () => {
    expect(normalizeHost(undefined)).toBeNull();
  });
});
