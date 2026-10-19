# Shared test vectors

One JSON file per rule that is implemented in both C# and TypeScript (ADR-03), for example `phone-normalization.json`.
Both test suites read the same file, so a change to one implementation that the other does not follow fails a test.

No vectors yet: the first shared rule arrives with the feature that needs it.
