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