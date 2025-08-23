# Contributing to Goa

Thank you for your interest in contributing to Goa! We welcome contributions from the community.

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature or bugfix
4. Make your changes following the project conventions
5. Test your changes
6. Submit a pull request

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/goa.git
cd goa

# Build the solution
dotnet build

# Run tests (requires Docker for LocalStack integration tests)
docker pull localstack/localstack:latest
dotnet test
```

**Note**: Tests require Docker to be running as they use LocalStack for AWS service integration testing.

## Project Conventions

### Code Style
- Follow existing code conventions and patterns in the codebase
- Use meaningful names for variables, methods, and classes
- Write XML documentation comments for public APIs
- Ensure code is compatible with Native AOT compilation

### Architecture
- Keep dependencies minimal and focused
- Maintain compatibility with .NET 9.0 and .NET 10.0
- Follow the existing patterns for AWS service clients
- Use source generators where appropriate for performance

### Testing
- Write unit tests for new functionality
- Use LocalStack for integration tests with AWS services
- Ensure all tests pass before submitting PR

## Pull Request Guidelines

### Size and Scope
- **Keep PRs focused and small** - ideally under 100 files changed
- Submit one feature or fix per PR
- If you have a large change, consider breaking it into smaller PRs

### PR Requirements
- Include a clear description of what the PR does
- Reference any related issues
- Add tests for new functionality
- Update documentation as needed
- Ensure CI/CD pipeline passes

## Reporting Issues

When reporting issues, please include:

- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Code samples if applicable

## Questions?

If you have questions about contributing, please:

1. Check existing issues and discussions
2. Create a new issue with the "question" label
3. Join community discussions

Thank you for contributing to Goa!