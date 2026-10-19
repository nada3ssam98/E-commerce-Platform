/** Text direction of a locale's script. */
export type TextDirection = 'ltr' | 'rtl';

/** Languages written right to left among the platform locales (requirements §6: `ar`, `en`). */
const rightToLeftLanguages: ReadonlySet<string> = new Set(['ar']);

/** `ar`, `ar-EG` → `rtl`; `en`, `en-US` → `ltr`. */
export function textDirection(locale: string): TextDirection {
  const language = locale.split('-')[0].toLowerCase();
  return rightToLeftLanguages.has(language) ? 'rtl' : 'ltr';
}
