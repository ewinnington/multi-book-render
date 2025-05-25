# Markdown Basics

Multi-Book Renderer supports all standard markdown features and enhances them with additional capabilities. Let's explore what you can do!

## Text Formatting

You can use all the standard text formatting options:

**Bold text** and *italic text* work as expected. You can also use ~~strikethrough~~ text.

### Code

Inline `code` looks great, and code blocks are beautifully highlighted:

```javascript
function greetReader(name) {
    console.log(`Hello, ${name}! Welcome to MBR!`);
    return `Greeting sent to ${name}`;
}

// This is executable! Try clicking the "Run" button above.
greetReader("Reader");
```

```python
# Python code is also supported
def calculate_fibonacci(n):
    if n <= 1:
        return n
    return calculate_fibonacci(n-1) + calculate_fibonacci(n-2)

# Calculate the 10th Fibonacci number
result = calculate_fibonacci(10)
print(f"The 10th Fibonacci number is: {result}")
```

## Mathematics

MBR includes full LaTeX math support using KaTeX:

### Inline Math
The quadratic formula is $x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}$.

### Display Math
$$
\int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
$$

### Complex Equations
$$
\begin{align}
\nabla \times \vec{\mathbf{B}} -\, \frac1c\, \frac{\partial\vec{\mathbf{E}}}{\partial t} &= \frac{4\pi}{c}\vec{\mathbf{j}} \\
\nabla \cdot \vec{\mathbf{E}} &= 4 \pi \rho \\
\nabla \times \vec{\mathbf{E}}\, +\, \frac1c\, \frac{\partial\vec{\mathbf{B}}}{\partial t} &= \vec{\mathbf{0}} \\
\nabla \cdot \vec{\mathbf{B}} &= 0
\end{align}
$$

## Lists and Tables

### Unordered Lists
- First item
- Second item
  - Nested item
  - Another nested item
- Third item

### Ordered Lists
1. First step
2. Second step
3. Third step

### Tables

| Feature | Supported | Notes |
|---------|-----------|-------|
| Markdown | ✅ | Full GFM support |
| Math | ✅ | KaTeX rendering |
| Code Execution | ✅ | JS, Python, C# |
| Mermaid Diagrams | ✅ | Coming in next chapter |
| Images | ✅ | Local and remote |

## Blockquotes

> "The best way to learn is by doing. MBR makes it easy to combine theory with practice through executable code examples."
> 
> — MBR Development Team

## Links and References

You can create [links to other chapters](03-advanced-features) or [external websites](https://github.com).

## What's Next?

In the next chapter, we'll explore advanced features like Mermaid diagrams, interactive code execution, and more!
