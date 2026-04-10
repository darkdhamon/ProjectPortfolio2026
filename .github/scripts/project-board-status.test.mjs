import test from "node:test";
import assert from "node:assert/strict";

import {
  determineStatusName,
  extractIssueNumbers,
} from "./project-board-status.mjs";

test("extractIssueNumbers reads only closing issue references", () => {
  const body = `## Summary
Promote changes.

## Included Issues
- #5: implement auth
- #60: implement work history

Closes #70

## Release Notes
- references merged PR #74 for context
`;

  assert.deepEqual(extractIssueNumbers(body), [70]);
});

test("extractIssueNumbers captures supported closing keywords only", () => {
  const body = `Closes #6
Refs #33
Fixes #70`;

  assert.deepEqual(extractIssueNumbers(body), [6, 70]);
});

test("extractIssueNumbers ignores pull request references in prose", () => {
  const body = `## Summary
Promote dev to stage.

## Included Issues
- #69: add csrf protection

## Release Notes
- Includes Issue #69 via merged PR #81
`;

  assert.deepEqual(extractIssueNumbers(body), []);
});

test("extractIssueNumbers ignores non-closing issue references", () => {
  const body = `Related to #61
Issue #6 is ready for review.
See #33 for follow-up work.
Merged PR #70 already shipped separately.`;

  assert.deepEqual(extractIssueNumbers(body), []);
});

test("extractIssueNumbers supports multiple closing references in one statement", () => {
  const body = `Resolves #12, #13 and owner/repo#14`;

  assert.deepEqual(extractIssueNumbers(body), [12, 13, 14]);
});

test("determineStatusName maps open PR activity to In review", () => {
  const pullRequest = {
    state: "open",
    draft: false,
    base: {
      ref: "dev",
    },
    merged: false,
  };

  assert.equal(determineStatusName("opened", pullRequest), "In review");
  assert.equal(determineStatusName("reopened", pullRequest), "In review");
  assert.equal(determineStatusName("ready_for_review", pullRequest), "In review");
  assert.equal(determineStatusName("edited", pullRequest), "In review");
});

test("determineStatusName maps merged PRs by promotion branch", () => {
  assert.equal(
    determineStatusName("closed", {
      merged: true,
      base: {
        ref: "dev",
      },
    }),
    "In dev branch",
  );

  assert.equal(
    determineStatusName("closed", {
      merged: true,
      base: {
        ref: "stage",
      },
    }),
    "In stage branch",
  );

  assert.equal(
    determineStatusName("closed", {
      merged: true,
      base: {
        ref: "main",
      },
    }),
    "Done",
  );
});

test("determineStatusName ignores closed unmerged PRs", () => {
  assert.equal(
    determineStatusName("closed", {
      merged: false,
      base: {
        ref: "dev",
      },
    }),
    null,
  );
});
