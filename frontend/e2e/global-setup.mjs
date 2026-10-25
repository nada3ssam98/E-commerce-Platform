import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { startStack } from './stack.mjs';

export const logDirectory = fileURLToPath(new URL('../test-results/stack', import.meta.url));

/** Starts the stack once for all tests; Playwright calls the returned function as the teardown. */
export default async function globalSetup() {
  const stack = await startStack(logDirectory);

  // Read by the tests to prove that no key value reaches the browser or the logs, and the internal URL not the browser.
  process.env.E2E_KEY_VALUES = JSON.stringify(Object.values(stack.keys));
  process.env.E2E_INTERNAL_API_URL = stack.internalApi;
  process.env.E2E_LOG_DIRECTORY = join(logDirectory);

  return stack.stop;
}
