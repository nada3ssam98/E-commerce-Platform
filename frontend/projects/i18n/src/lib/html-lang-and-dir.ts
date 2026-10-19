import { DOCUMENT } from '@angular/common';
import { EnvironmentProviders, inject, LOCALE_ID, provideAppInitializer } from '@angular/core';
import { textDirection } from './text-direction';

/**
 * Sets `<html lang dir>` from the locale of the running build (requirements §6; T-1: one build per locale).
 * Runs on the server during SSR and in the browser, so the server-rendered HTML already carries both attributes.
 */
export function provideHtmlLangAndDir(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const locale = inject(LOCALE_ID);
    const html = inject(DOCUMENT).documentElement;
    html.lang = locale;
    html.dir = textDirection(locale);
  });
}
