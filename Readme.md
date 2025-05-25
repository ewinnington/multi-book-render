# Multi-book-render 

MBR is a markdown renderer that hosts books with:
- markdown 
- latex maths
- mermaid diagrams
- syntax highlighting
- image support
- Executable code blocks
- Book navigation

It is based on .net9

# Journey 

## User journey

1) connect to website. See book covers.  (optional : login) 
2) click on book cover, go to chapter 1, see all chapters as side panel for quick navigation, Next and Previous chapter at top and bottom of page.
3) chapter renders the markdown, latex, diagrams and images. 
4) any code - clearly indicated - can be run (this is advanced mode ...) 

## Admin journey
1) connect to website, login. 
2) Choose which books appear, for which users (all, specific user) 
3) Add book 
4) Edit book (title, cover colour) 
5) Add / Edit chapter - in Markdown (no rendering)
6) Save edits -> Each book is its own git repo and does a commit after changes (automessage datetime of edit) 
7) Any settings for code blocks. (?)

# Storage

MBR doesn not use a database. 
For the .md files, it goes to a folder on disk. Each book is a separate folder, with each chapter as a separate .md file.
For the images, it goes to a folder on disk. Each book is a separate folder, with each image in a subfolder called "assets".
For any settings, use .json files for storage in the book for per book settings and in a siteconfig.json for site wide settings, user.json for the users. This makes moving and backing up the site more efficient. 

