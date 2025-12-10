# Contributing to DNFileRAG

Thank you for your interest in contributing to DNFileRAG! This document provides guidelines and information for contributors.

## Code of Conduct

Please be respectful and constructive in all interactions. We welcome contributors of all experience levels.

## How to Contribute

### Reporting Issues

- Check existing issues before creating a new one
- Use the issue template if provided
- Include:
  - Clear description of the problem
  - Steps to reproduce
  - Expected vs actual behavior
  - Environment details (.NET version, OS, etc.)

### Suggesting Features

- Open an issue with the "feature request" label
- Describe the use case and proposed solution
- Be open to discussion and alternatives

### Submitting Pull Requests

1. **Fork** the repository
2. **Create a branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Make your changes** following our coding standards
4. **Write tests** for new functionality
5. **Run tests** locally:
   ```bash
   dotnet test
   ```
6. **Commit** with clear messages:
   ```bash
   git commit -m "Add feature: description of what was added"
   ```
7. **Push** and create a Pull Request

### Pull Request Guidelines

- Keep PRs focused on a single change
- Update documentation if needed
- Ensure all tests pass
- Follow existing code style
- Add tests for new features

## Development Setup

### Prerequisites

- .NET 9 SDK
- Docker (for Qdrant)
- Ollama (optional, for local testing)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/DNFileRAG.git
cd DNFileRAG

# Build
dotnet build

# Run tests
dotnet test

# Start dependencies
docker run -d -p 6333:6333 qdrant/qdrant

# Run the application
cd src/DNFileRAG
dotnet run
```

## Coding Standards

### General

- Follow C# naming conventions
- Use meaningful variable and method names
- Keep methods small and focused
- Write self-documenting code

### Architecture

- Follow clean architecture principles
- Keep domain logic in `DNFileRAG.Core`
- External integrations go in `DNFileRAG.Infrastructure`
- Use dependency injection

### Testing

- Write unit tests for business logic
- Use meaningful test names
- Follow Arrange-Act-Assert pattern

## Project Structure

```
src/
├── DNFileRAG/                 # Web API (entry point)
├── DNFileRAG.Core/            # Domain models, interfaces
└── DNFileRAG.Infrastructure/  # External implementations

tests/
└── DNFileRAG.Tests/           # Unit tests
```

## Questions?

Feel free to open an issue for questions or join discussions.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
