# Test Asset Path Compatibility

This chapter tests different image path formats to verify compatibility with external markdown editors.

## Direct Image Path (Original Format)
This should work as before:
![kitten direct](kitten-papers.png)

## Assets Prefixed Path (External Editor Format)
This should now work after the fix:
![kitten with assets prefix](assets/kitten-papers.png)

## Both formats with sizing attributes
Direct with size:
![kitten direct sized](kitten-papers.png){width=200px}

Assets prefixed with size:
![kitten assets sized](assets/kitten-papers.png){width=200px}

Both should generate the same final URL and display the same image.
