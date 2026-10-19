import { normalizeHost } from './host';

describe('normalizeHost', () => {
  it.each([
    ['brand-a.localhost', 'brand-a.localhost'],
    ['Brand-A.localhost:443', 'brand-a.localhost'],
    ['brand-a.localhost.', 'brand-a.localhost'],
    [' brand-a.example.com ', 'brand-a.example.com'],
  ])('%s → %s', (raw, expected) => {
    expect(normalizeHost(raw)).toBe(expected);
  });

  it.each([undefined, '', 'brand a.localhost', 'brand-a.localhost:http', '-brand.localhost', 'brand_a.localhost', 'a..b', `${'a'.repeat(64)}.localhost`])(
    'rejects %s',
    (raw) => {
      expect(normalizeHost(raw)).toBeNull();
    },
  );
});
