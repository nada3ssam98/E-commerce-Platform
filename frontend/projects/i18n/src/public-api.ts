/*
 * Public API of @platform/i18n: locale, direction, and (later) formatting helpers shared by both applications.
 * Components never format numbers, dates, or money themselves (ui-design §5).
 */

export * from './lib/html-lang-and-dir';
export * from './lib/text-direction';
