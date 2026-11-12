---
name: feature-cycle
description: Complete a Unity feature from a description, chat context, or task reference in a dedicated branch, including implementation, Editor tests, fixes, temporary test cleanup, and a Markdown status report. Use when asked to implement a feature through this workflow; do not use for a simple question, analysis, or running existing tests alone.
---

# Feature Cycle

Bring the requested Unity feature to a verified state in a dedicated branch and leave a Markdown status report for the user to review. Invoking this skill requests implementation, running Unity, writing temporary tests, fixing issues found during verification, and removing those temporary tests afterward. Environment permission requirements still apply.

## Input and Context

- Accept the feature description from the user's message, established context, or a referenced file or task item. The user may specify the base branch, branch name, feature folder, and report path, but these are optional.
- Reuse requirements and architecture already read. Read only missing context and affected code; do not restart a full project investigation when the relevant information is already available.
- Before making changes, recheck mutable facts: Git status, actual branches, local rules, and the Editor version pinned in `ProjectSettings/ProjectVersion.txt`.
- Derive verifiable acceptance criteria from the request. Make reasonable implementation decisions independently and record them in the report. Ask only for missing information without which the required behavior cannot be determined.
- A request for this workflow includes testing, even when project defaults suggest not writing tests or not running the local Editor. Continue following the project's other rules.

## Branch and Feature Folder

- Create a dedicated `feature/<short-name>` branch from the user-specified base or the project's actual integration branch. If `dev` does not exist, use the project's established alternative rather than inventing a new main branch.
- Preserve other people's changes and unfinished work. Isolate the work in a separate worktree when necessary. Continue an existing branch for the same task only after inspecting its contents; do not overwrite someone else's branch with the same name.
- Create the feature folder according to existing conventions. If no report folder is specified, use `Docs/Features/<short-name>/Status.md`. Place code in the appropriate architectural folders, without duplicating the source tree under `Docs`.

## Implementation and Verification

1. Implement the requested behavior according to the project's architecture. Do not expand the task into a general audit or unrelated refactoring.
2. Write temporary checks for the acceptance criteria and likely regressions. Test observable behavior rather than reproducing the implementation in a test.
3. Use EditMode for logic and validation. For input, animations, scenes, Addressables, and lifecycle behavior, add PlayMode or Editor checks and an appropriate gameplay smoke check. When changing dependency injection or states, verify actual composition and transitions rather than only individually constructed services.
4. Run the correct pinned Unity version. A closed project can be checked in batch mode. If the Editor is already open, use available controls for that Editor session; do not close it with the user's unsaved work.
5. Save fresh XML results and logs for every run in a separate temporary folder. Check the process exit code, actual test execution, unexpected skips, and result status. A zero exit code without executed checks is not a success.
6. Read logs for compilation errors, exceptions, and unexpected runtime errors. Assess warnings by their impact on the feature; expected errors from negative tests must be explicitly associated with a specific test. Do not hide real errors by globally disabling log checks.
7. Fix the causes of failures and rerun affected checks. Before finishing, run an appropriate existing suite and an integration scenario. Do not weaken assertions, disable tests, or change expected behavior merely to obtain passing results. Update old expectations only when they conflict with the agreed new behavior.

Continue the cycle while meaningful fixes remain and acceptance criteria are unmet. Resolve fixable environment and test harness issues within the task's scope. If an external blocker requires unavailable access, a license, or a user decision, preserve the work and record the specific reason. Do not report failed verification as successful or repeat the same failure without a new reason to rerun it.

## Temporary Test Cleanup

- By default, tests created specifically by this invocation to verify the feature are temporary. After a successful run, preserve the results, then remove only those tests, their own helper files, and associated `.meta` files. If the user asks to keep the tests, retain them instead.
- Do not delete existing tests. Remove temporary verification methods added to them after success; retain necessary expectation changes for the new behavior. Do not revert user changes or useful fixes to the existing test harness.
- Create temporary Unity assets through the Editor in unique folders. Restore content, settings, and user progress modified by tests. Do not invent GUIDs or manually edit Unity-serialized assets.
- After removing temporary checks, recompile the final project and run appropriate remaining tests or a smoke check. Record results before and after cleanup separately. Verify that production code does not depend on deleted test classes. If final verification finds an error, return to the fix-and-verify cycle; removing tests does not mean the work is complete.

## Status and Completion

At the end, create or update one `Status.md` file or the Markdown file specified by the user. Write concisely and factually in the user's language:

- Status: "ready for review", "partially completed", or "blocked".
- Feature, source of requirements, assumptions, branch, and base branch.
- What was implemented and which significant issues were found and fixed during verification.
- What actually ran: Editor version, suites or scenarios, passing and failing test counts, log status, and result paths.
- Which temporary checks were removed and what was successfully verified on the final tree afterward.
- Verification limits and remaining manual steps, if any. Passing automated tests do not replace device, performance, or visual checks when those are part of the feature's acceptance criteria.

On successful completion, commit the changes and report on the feature branch according to project conventions. Exclude unrelated files and temporary data. Leave the branch for the user to review; push, open a pull request, or merge only when separately requested. If blocked, preserve completed work and the report without marking the feature ready.

In the response, include the branch, status report path, verification results, and significant limitations. Do not stop after implementation without verification and a report.
