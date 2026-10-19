import { textDirection } from './text-direction';

describe('textDirection', () => {
  it.each([
    ['ar', 'rtl'],
    ['ar-EG', 'rtl'],
    ['AR', 'rtl'],
    ['en', 'ltr'],
    ['en-US', 'ltr'],
  ] as const)('%s is %s', (locale, expected) => {
    expect(textDirection(locale)).toBe(expected);
  });
});
