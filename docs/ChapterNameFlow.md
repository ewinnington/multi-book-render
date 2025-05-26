# Chapter Name Data Flow

```mermaid
graph TD
    A[User enters chapter title in UI] --> B[AdminController (Create/Edit)]
    B --> C[ChapterService]
    C --> D[ApplyTitleHeader writes Markdown file]
    D --> E[book.json updated via BookService]
    E --> F[ManageChapters loads Book]
    F --> G[UI lists chapters with titles]
```
