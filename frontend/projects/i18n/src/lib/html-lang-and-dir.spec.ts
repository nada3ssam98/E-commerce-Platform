import { DOCUMENT } from '@angular/common';
import { ApplicationInitStatus, LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHtmlLangAndDir } from './html-lang-and-dir';

async function htmlFor(locale: string): Promise<HTMLElement> {
  TestBed.configureTestingModule({
    providers: [{ provide: LOCALE_ID, useValue: locale }, provideHtmlLangAndDir()],
  });
  await TestBed.inject(ApplicationInitStatus).donePromise;
  return TestBed.inject(DOCUMENT).documentElement;
}

describe('provideHtmlLangAndDir', () => {
  it('sets lang="ar" dir="rtl" for Arabic', async () => {
    const html = await htmlFor('ar');

    expect(html.lang).toBe('ar');
    expect(html.dir).toBe('rtl');
  });

  it('sets lang="en" dir="ltr" for English', async () => {
    const html = await htmlFor('en');

    expect(html.lang).toBe('en');
    expect(html.dir).toBe('ltr');
  });
});
