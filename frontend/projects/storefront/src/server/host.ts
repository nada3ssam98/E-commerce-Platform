const label = /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/;

/**
 * Normalizes a host as received from the edge: lowercase, no port, no trailing dot, valid DNS syntax
 * (domain-model §0 Hostname; ADR-04 rules 3 and 13). Returns `null` when the value is not a valid host name.
 */
export function normalizeHost(raw: string | undefined): string | null {
  if (raw === undefined) {
    return null;
  }
  let host = raw.trim().toLowerCase();
  const portSeparator = host.lastIndexOf(':');
  if (portSeparator !== -1) {
    const port = host.slice(portSeparator + 1);
    if (!/^\d{1,5}$/.test(port)) {
      return null;
    }
    host = host.slice(0, portSeparator);
  }
  if (host.endsWith('.')) {
    host = host.slice(0, -1);
  }
  if (host.length === 0 || host.length > 253 || !host.split('.').every((part) => label.test(part))) {
    return null;
  }
  return host;
}
