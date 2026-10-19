import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('Storefront App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  async function render(respond: (backend: HttpTestingController) => void): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    respond(TestBed.inject(HttpTestingController));
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the placeholder heading and the API status', async () => {
    const element = await render((backend) => backend.expectOne('/api/health').flush({ status: 'Healthy' }));

    expect(element.querySelector('h1')?.textContent).toBe('Welcome');
    expect(element.querySelector('[data-testid="api-health"]')?.textContent).toBe('Healthy');
  });

  it('shows "unavailable" when the API cannot be reached', async () => {
    const element = await render((backend) =>
      backend.expectOne('/api/health').flush(null, { status: 503, statusText: 'Service Unavailable' }),
    );

    expect(element.querySelector('[data-testid="api-health"]')?.textContent).toBe('unavailable');
  });
});
