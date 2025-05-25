C# .NET 9 Markdown Book Renderer - Development Guide
Application Architecture Overview
High-Level Architecture

Frontend: ASP.NET Core MVC with Razor Pages and JavaScript enhancements
Backend: .NET 9 Web API controllers for data operations
Storage: File-based system (no database)

Markdown files in organized folder structure
JSON configuration files for settings
Git repositories for version control per book
Static assets (images, covers) in wwwroot



Project Structure
BookRenderer/
├── BookRenderer.Web/           # Main web application
├── BookRenderer.Core/          # Business logic and models
├── BookRenderer.Services/      # File operations, Git, rendering
├── BookRenderer.Admin/         # Admin panel (could be separate project or area)
└── Data/                      # File storage
    ├── Books/                 # Each book as separate folder/git repo
    ├── Users/                 # User settings (JSON files)
    ├── Config/                # Global settings
    └── wwwroot/              # Static assets
Development Tasks Breakdown
Phase 1: Core Infrastructure Setup
Task 1.1: Project Setup and Dependencies

Create .NET 9 web application solution
Add NuGet packages:

Markdig (markdown processing with math extensions)
LibGit2Sharp (Git operations)
System.Text.Json (JSON handling)
Authentication packages


Configure dependency injection
Set up project structure and namespaces

Task 1.2: File System Architecture Design

Design folder structure for books and chapters
Create JSON schema for:

Book metadata (title, cover, author, visibility settings)
User profiles and permissions
Global application settings
Chapter ordering and metadata


Implement file naming conventions and organization

Task 1.3: Configuration and Settings Management

Create configuration models for all JSON settings
Implement settings service for reading/writing configurations
Set up environment-specific configurations (development, production)
Create default settings and initialization logic

Phase 2: Core Models and Services
Task 2.1: Domain Models

Book Model: Title, description, cover info, chapters list, visibility settings, git repo path
Chapter Model: Title, filename, order, content, metadata
User Model: Username, permissions, assigned books, preferences
BookAccess Model: User-to-book permissions mapping
CodeBlockSettings Model: Execution permissions, timeout settings, allowed languages

Task 2.2: File System Service

Create service for reading/writing markdown files
Implement book discovery and loading from folder structure
Create chapter ordering and navigation logic
Handle file watching for real-time updates (optional)

Task 2.3: Git Integration Service

Initialize Git repositories for new books
Implement commit functionality with auto-generated messages
Add basic Git operations (status, history)
Handle Git repository management (create, clone, maintain)

Task 2.4: Markdown Processing Service

Configure Markdig pipeline with extensions:

GitHub Flavored Markdown
Mathematics (LaTeX support)
Tables and advanced formatting
Custom extensions for code block identification


Implement custom renderers for special elements
Create preprocessing for Mermaid diagram detection

Phase 3: User Interface - Reader Mode
Task 3.1: Book Listing Page

Create responsive book grid/list view
Implement book cover display (color-based or image)
Add filtering and search functionality
Handle user-specific book visibility
Create book selection and navigation

Task 3.2: Chapter Reading Interface

Design chapter layout with sidebar navigation
Implement responsive design for mobile/desktop
Create chapter navigation (previous/next buttons)
Add table of contents generation from markdown headers
Implement reading progress tracking (client-side)

Task 3.3: Content Rendering System

Integrate Markdig for server-side markdown processing
Add client-side math rendering (KaTeX integration)
Implement Mermaid diagram rendering
Handle image display and optimization
Create print-friendly styles

Task 3.4: Interactive Code Blocks

Identify and mark executable code blocks during rendering
Create JavaScript framework for code execution:

Syntax highlighting integration
Execution buttons and output display
Language-specific execution engines


Implement security measures and sandboxing
Add execution settings and permissions checking

Phase 4: User Interface - Admin Mode
Task 4.1: Authentication and Authorization

Implement simple authentication system (file-based user storage)
Create admin vs. regular user role distinction
Add login/logout functionality
Implement session management

Task 4.2: Admin Dashboard

Create admin-only area with separate layout
Design book management interface
Add user management capabilities
Create system settings and configuration UI

Task 4.3: Book Management Interface

Book Creation: Form for new book setup, Git repo initialization
Book Editing: Edit book metadata, cover settings, visibility
Book Assignment: Assign books to specific users or make public
Book Deletion: Safe removal with Git history preservation

Task 4.4: Chapter Management Interface

Chapter Editor: Raw markdown editing interface
Chapter Organization: Drag-and-drop chapter ordering
Chapter Metadata: Title, description, publishing status
Preview Functionality: Real-time preview of markdown rendering

Task 4.5: Git Integration UI

Display commit history for each book
Show current Git status and changes
Manual commit functionality with custom messages
Basic diff viewing for changes

Phase 5: Advanced Features
Task 5.1: Code Execution Framework

JavaScript Execution: Safe client-side evaluation
Python Execution: Pyodide integration or server-side sandboxing
C# Execution: Roslyn scripting or containerized execution
Security Implementation: Timeouts, resource limits, code validation

Task 5.2: Search and Navigation

Implement full-text search across all accessible books
Create advanced filtering (by author, tags, recent updates)
Add bookmark/favorites functionality
Implement reading history and resume functionality

Task 5.3: Performance Optimization

Implement markdown caching system
Add lazy loading for large books
Optimize image loading and serving
Create efficient chapter preloading

Phase 6: Deployment and Operations
Task 6.1: Docker Configuration

Create multi-stage Dockerfile for .NET 9 application
Configure for Ubuntu 24.04 deployment
Set up volume mounting for data persistence
Add health checks and monitoring

Task 6.2: Production Setup

Configure reverse proxy settings (Nginx integration)
Set up SSL/HTTPS configuration
Implement logging and error handling
Create backup and restore procedures

Task 6.3: Security Hardening

Implement file system security measures
Add input validation and sanitization
Configure proper authentication and authorization
Set up code execution sandboxing

Detailed Task Implementation Strategy
File System Organization Strategy
Data/
├── Books/
│   ├── BookId1/
│   │   ├── .git/                 # Git repository
│   │   ├── book.json            # Book metadata
│   │   ├── chapters/
│   │   │   ├── 01-introduction.md
│   │   │   ├── 02-basics.md
│   │   │   └── ...
│   │   └── assets/              # Book-specific images
│   └── BookId2/
├── Config/
│   ├── app-settings.json        # Global settings
│   ├── users.json              # User accounts
│   └── permissions.json        # User-book assignments
└── Temp/                       # Temporary files for processing
Service Layer Architecture

IBookService: Book CRUD operations, discovery, metadata management
IChapterService: Chapter operations, ordering, content management
IUserService: User authentication, permissions, profile management
IGitService: Repository operations, commit management, history
IMarkdownService: Processing, rendering, caching
ICodeExecutionService: Safe code execution across languages
IFileSystemService: Low-level file operations, watching, caching

API Endpoints Design
Reader API:

GET /api/books - List accessible books
GET /api/books/{id} - Get book details
GET /api/books/{id}/chapters/{chapter} - Get rendered chapter
POST /api/code/execute - Execute code blocks

Admin API:

GET /api/admin/books - List all books (admin view)
POST /api/admin/books - Create new book
PUT /api/admin/books/{id} - Update book metadata
POST /api/admin/books/{id}/chapters - Add new chapter
PUT /api/admin/chapters/{id} - Update chapter content
POST /api/admin/git/commit - Manual commit

Security Considerations per Task

File Access: Implement path traversal protection
Code Execution: Sandboxing, timeouts, resource limits
Authentication: Secure session management, password hashing
Input Validation: Markdown content sanitization, XSS prevention
Git Operations: Validate repository integrity, prevent malicious commits

Testing Strategy per Phase

Unit Tests: Service layer logic, markdown processing, Git operations
Integration Tests: File system operations, authentication flows
End-to-End Tests: Complete user journeys, admin workflows
Security Tests: Code execution safety, file access controls
Performance Tests: Large book loading, concurrent user access

This development approach ensures a robust, maintainable application that meets all your specified requirements while maintaining security and performance standards.