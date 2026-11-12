---
name: chat-notes
description: Save a concise summary of the current chat as Markdown in the project's Docs folder, with a date and short topic in the filename. Use when asked to summarize or save the current conversation's context to a file; do not use for general project documentation without such a request.
---

# Chat Notes

Save the essential context of the current chat in one short Markdown file so the user can recover decisions and continue the work later.

## Saving

- Use the current conversation and any available summary of it. Read additional files only to verify significant facts that the conversation does not establish. Do not investigate the whole project again.
- Write in the user's language. Preserve important outcomes from the entire available chat, not only the latest message. Consolidate repetition, clarifications, and corrections into the current decisions; mention abandoned plans only when needed for understanding.
- Use the `docs` or `Docs` folder already established in the project. If neither exists, create `Docs` in the project root. A user-specified path takes precedence.
- Read the current local date from the environment and name the file `YYYY-MM-DD-short-topic.md`. Use 2-5 short, filename-safe words describing the chat's main subject, for example `2026-11-05-puzzle-features-and-skills.md`.
- Do not overwrite an existing file: if the name is taken, append the local time as `HHmmss` or a numeric suffix. Update an existing summary only at the user's explicit request.

## Content

Include the date and a clear title. Then briefly record:

- Context: what the user wanted to achieve.
- Decisions, constraints, and important preferences.
- Completed work: significant changes, branches, commits, files, and created tools, if discussed.
- Checks and their actual results; distinguish anything unverified or blocked.
- Remaining tasks and the next step, if actually established in the chat.

Include only relevant items, usually 200-500 words and fewer for a short chat. This is a summary, not a transcript: omit full logs, repeated failed attempts, and long quotations. Preserve specific branch names, paths, and numbers when they are needed to continue the work.

Distinguish proposals, completed changes, and confirmed checks. Do not invent missing history, results, or agreements. If earlier context is unavailable and that matters, briefly state the limitation. Exclude secrets, tokens, passwords, and private data from test saves.

## Completion

Read the finished file and check the date, paths, and consistency. This skill creates only a summary: do not change code or the Git branch, or commit, push, or merge without a separate request. In the response, link to the saved file and describe its contents in one sentence.
