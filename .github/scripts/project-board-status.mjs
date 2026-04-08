import fs from "node:fs";
import { pathToFileURL } from "node:url";

const DEFAULT_CONFIG = {
  projectId: process.env.PROJECT_ID ?? "PVT_kwHOACEO7s4BUEGN",
  statusFieldId:
    process.env.PROJECT_STATUS_FIELD_ID ?? "PVTSSF_lAHOACEO7s4BUEGNzhBOxOM",
  statusOptionIds: {
    "In review": process.env.PROJECT_STATUS_IN_REVIEW ?? "4f516d6b",
    "In dev branch": process.env.PROJECT_STATUS_IN_DEV_BRANCH ?? "befac53f",
    "In stage branch":
      process.env.PROJECT_STATUS_IN_STAGE_BRANCH ?? "9eb7c4ca",
    Done: process.env.PROJECT_STATUS_DONE ?? "7602d953",
  },
};

export function extractIssueNumbers(body) {
  if (!body) {
    return [];
  }

  const lines = body.split(/\r?\n/);
  const includedIssuesHeaderIndex = lines.findIndex((line) =>
    /^## Included Issues\b/.test(line),
  );
  let sourceText = body;

  if (includedIssuesHeaderIndex >= 0) {
    const includedIssueLines = [];

    for (let index = includedIssuesHeaderIndex + 1; index < lines.length; index += 1) {
      if (/^##\s/.test(lines[index])) {
        break;
      }

      includedIssueLines.push(lines[index]);
    }

    sourceText = includedIssueLines.join("\n");
  }

  const issueNumbers = new Set();
  const genericIssuePattern = /#(\d+)\b/g;

  for (const match of sourceText.matchAll(genericIssuePattern)) {
    issueNumbers.add(Number(match[1]));
  }

  if (issueNumbers.size > 0) {
    return [...issueNumbers];
  }

  const keywordPattern =
    /\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?|ref(?:er(?:s|enced?)?)?)\s*:?\s+#(\d+)\b/gi;

  for (const match of body.matchAll(keywordPattern)) {
    issueNumbers.add(Number(match[1]));
  }

  return [...issueNumbers];
}

export function determineStatusName(action, pullRequest) {
  if (!pullRequest) {
    return null;
  }

  if (
    ["opened", "reopened", "ready_for_review"].includes(action) ||
    (action === "edited" && pullRequest.state === "open" && !pullRequest.draft)
  ) {
    return "In review";
  }

  if (action === "closed" && pullRequest.merged) {
    if (pullRequest.base?.ref === "dev") {
      return "In dev branch";
    }

    if (pullRequest.base?.ref === "stage") {
      return "In stage branch";
    }

    if (pullRequest.base?.ref === "main") {
      return "Done";
    }
  }

  return null;
}

async function graphqlRequest(query, variables, token) {
  const response = await fetch("https://api.github.com/graphql", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      "User-Agent": "project-board-status-workflow",
    },
    body: JSON.stringify({
      query,
      variables,
    }),
  });

  const payload = await response.json();

  if (!response.ok || payload.errors) {
    throw new Error(
      `GitHub GraphQL request failed: ${JSON.stringify(payload.errors ?? payload)}`,
    );
  }

  return payload.data;
}

async function getIssueProjectItem(owner, repo, issueNumber, projectId, token) {
  const query = `
    query GetIssueProjectItem($owner: String!, $repo: String!, $issueNumber: Int!) {
      repository(owner: $owner, name: $repo) {
        issue(number: $issueNumber) {
          id
          number
          title
          projectItems(first: 20) {
            nodes {
              id
              project {
                id
              }
            }
          }
        }
      }
    }
  `;

  const data = await graphqlRequest(
    query,
    {
      owner,
      repo,
      issueNumber,
    },
    token,
  );

  const issue = data.repository?.issue;

  if (!issue) {
    return null;
  }

  const existingItem = issue.projectItems.nodes.find(
    (item) => item.project?.id === projectId,
  );

  return {
    issueId: issue.id,
    issueNumber: issue.number,
    title: issue.title,
    itemId: existingItem?.id ?? null,
  };
}

async function addIssueToProject(projectId, issueId, token) {
  const mutation = `
    mutation AddIssueToProject($projectId: ID!, $issueId: ID!) {
      addProjectV2ItemById(input: { projectId: $projectId, contentId: $issueId }) {
        item {
          id
        }
      }
    }
  `;

  const data = await graphqlRequest(
    mutation,
    {
      projectId,
      issueId,
    },
    token,
  );

  return data.addProjectV2ItemById.item.id;
}

async function updateProjectItemStatus(
  projectId,
  itemId,
  fieldId,
  optionId,
  token,
) {
  const mutation = `
    mutation UpdateProjectItemStatus(
      $projectId: ID!,
      $itemId: ID!,
      $fieldId: ID!,
      $optionId: String!
    ) {
      updateProjectV2ItemFieldValue(
        input: {
          projectId: $projectId,
          itemId: $itemId,
          fieldId: $fieldId,
          value: { singleSelectOptionId: $optionId }
        }
      ) {
        projectV2Item {
          id
        }
      }
    }
  `;

  await graphqlRequest(
    mutation,
    {
      projectId,
      itemId,
      fieldId,
      optionId,
    },
    token,
  );
}

export async function run({
  event,
  owner,
  repo,
  token,
  config = DEFAULT_CONFIG,
}) {
  const pullRequest = event.pull_request;
  const statusName = determineStatusName(event.action, pullRequest);

  if (!statusName) {
    console.log(
      `No board status update needed for action '${event.action}' on PR #${pullRequest?.number ?? "unknown"}.`,
    );
    return;
  }

  const issueNumbers = extractIssueNumbers(pullRequest?.body ?? "");

  if (issueNumbers.length === 0) {
    console.log(
      `No linked issues found in PR #${pullRequest.number}; skipping project board update.`,
    );
    return;
  }

  const statusOptionId = config.statusOptionIds[statusName];

  if (!statusOptionId) {
    throw new Error(`No project status option ID configured for '${statusName}'.`);
  }

  console.log(
    `Updating ${issueNumbers.length} issue(s) from PR #${pullRequest.number} to '${statusName}'.`,
  );

  for (const issueNumber of issueNumbers) {
    const issueProjectItem = await getIssueProjectItem(
      owner,
      repo,
      issueNumber,
      config.projectId,
      token,
    );

    if (!issueProjectItem) {
      console.warn(`Issue #${issueNumber} was not found in ${owner}/${repo}; skipping.`);
      continue;
    }

    let itemId = issueProjectItem.itemId;

    if (!itemId) {
      itemId = await addIssueToProject(config.projectId, issueProjectItem.issueId, token);
      console.log(`Added issue #${issueNumber} to the project board.`);
    }

    await updateProjectItemStatus(
      config.projectId,
      itemId,
      config.statusFieldId,
      statusOptionId,
      token,
    );

    console.log(`Updated issue #${issueNumber} to '${statusName}'.`);
  }
}

async function main() {
  const token = process.env.PROJECT_AUTOMATION_TOKEN || process.env.GITHUB_TOKEN;

  if (!token) {
    throw new Error(
      "PROJECT_AUTOMATION_TOKEN or GITHUB_TOKEN must be set for project board automation.",
    );
  }

  const eventPath = process.env.GITHUB_EVENT_PATH;
  const repository = process.env.GITHUB_REPOSITORY;

  if (!eventPath || !repository) {
    throw new Error("GITHUB_EVENT_PATH and GITHUB_REPOSITORY must be available.");
  }

  const event = JSON.parse(fs.readFileSync(eventPath, "utf8"));
  const [owner, repo] = repository.split("/");

  await run({
    event,
    owner,
    repo,
    token,
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch((error) => {
    console.error(error);
    process.exitCode = 1;
  });
}
