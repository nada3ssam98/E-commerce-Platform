import { matchesAnyKey } from './key-match';

const current = 'current-key-0123456789abcdef0123456789';
const previous = 'previous-key-0123456789abcdef012345678';

describe('matchesAnyKey', () => {
  it('accepts the current and the previous key', () => {
    expect(matchesAnyKey(current, [current, previous])).toBe(true);
    expect(matchesAnyKey(previous, [current, previous])).toBe(true);
  });

  it.each([undefined, '', 'wrong', `${current}x`, current.slice(0, -1)])('rejects %s', (presented) => {
    expect(matchesAnyKey(presented, [current, previous])).toBe(false);
  });

  it('rejects everything when no key is accepted', () => {
    expect(matchesAnyKey('', [])).toBe(false);
    expect(matchesAnyKey(current, [])).toBe(false);
  });
});
