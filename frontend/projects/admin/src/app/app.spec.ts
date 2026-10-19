import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Admin App', () => {
  it('renders the placeholder heading', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h1')?.textContent).toBe('Store administration');
  });
});
