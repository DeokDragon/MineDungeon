# Unity Development Rules

## Token Optimization
- Read only files relevant to the task.
- Do not scan the entire project unless necessary.
- Avoid repeated MCP calls and file reads.
- Keep responses concise.

## Unity MCP
- Use MCP for scenes, GameObjects and prefabs.
- Batch similar operations when possible.

## Code Safety
- Preserve existing functionality.
- Do not modify unrelated files.
- Check compilation errors after changes.
- Never claim tests passed unless executed.

## Reporting
- Summarize changes and errors briefly.

## Code Reuse & Script Management
- Check related existing scripts, components, and shared utilities before implementing a feature.
- Reuse or extend existing functionality when appropriate.
- Do not create duplicate scripts that perform the same responsibility.
- Create new scripts only when a separate responsibility or clear technical need justifies them.
- Do not refactor, rename, move, merge, or delete existing scripts without task-specific authorization.
- Check references before proposing script removal, including scenes, prefabs, ScriptableObjects, and runtime usage.
- Report newly created scripts separately from modified scripts.

## Design Documents
- Follow the latest explicitly approved user decisions.
- Refer to Docs/GDD.md and only the relevant Docs/Systems documents.
- Check Docs/WORK_STATUS.md only when implementation history or test status is needed.
- Distinguish approved designs, temporary test values, and implementation reports.
- Do not implement unapproved features or change confirmed design rules.

## Project Safety
- Preserve existing scenes, prefabs, serialized data, .meta files, GUIDs, and unrelated Git changes.
- Avoid creating new managers, singletons, or systems when existing architecture can support the feature.
- Report design conflicts or changes requiring major architectural modifications before proceeding.
- Never commit or push without explicit user approval.