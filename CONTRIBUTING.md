# Contributing

## Architecture Guidelines

This project follows a simple layered Clean Architecture:

- **Domain**: entities only
- **Application**: services, interfaces, DTOs
- **Infrastructure**: EF Core, repositories
- **API**: controllers only

## Required Practices

- Do **not** use CQRS
- Do **not** use MediatR
- Do **not** use microservices patterns
- Avoid over-engineering
- Use a simple service-based architecture
- Do not create unnecessary abstractions
- Do not add a generic repository unless clearly needed
- Create only the classes required by the feature

## Coding Style

- Keep code clean, readable, and minimal
- Use proper naming
- Avoid advanced patterns unless strictly necessary