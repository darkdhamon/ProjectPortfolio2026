import test from "node:test";
import assert from "node:assert/strict";

import {
  determineStatusName,
  extractIssueNumbers,
} from "./project-board-status.mjs";

test("extractIssueNumbers reads issue numbers from the Included Issues section only", () => {
  const body = `## Summary
Promote changes.

## Included Issues
- #5: implement auth
- #60: implement work history

## Release Notes
- references merged PR #74 for context
`;

  assert.deepEqual(extractIssueNumbers(body), [5, 60]);
});

test("extractIssueNumbers falls back to generic references when no Included Issues section exists", () => {
  const body = `Closes #6
Refs #33
Fixes #70`;

  assert.deepEqual(extractIssueNumbers(body), [6, 33, 70]);
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
