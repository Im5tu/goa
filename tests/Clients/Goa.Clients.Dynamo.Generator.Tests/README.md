# Goa.Clients.Dynamo.Generator.Tests

Comprehensive unit test suite for the DynamoDB source generator project using TUnit framework.

## Test Structure

### Type Handlers Tests
- **PrimitiveTypeHandlerTests**: Tests for all primitive types (bool, int, string, decimal, etc.)
- **DateOnlyTypeHandlerTests**: Tests for DateOnly type with ISO 8601 formatting
- **TimeOnlyTypeHandlerTests**: Tests for TimeOnly type with high-precision formatting
- **EnumTypeHandlerTests**: Tests for enum serialization and MissingAttributeException scenarios
- **CollectionTypeHandlerTests**: Tests for IEnumerable collections
- **ComplexTypeHandlerTests**: Tests for nested object serialization
- **TypeHandlerRegistryTests**: Tests for handler registration and priority ordering

### Attribute Handler Tests
- **DynamoModelAttributeHandlerTests**: Tests for [DynamoModel] attribute parsing
- **GSIAttributeHandlerTests**: Tests for [GlobalSecondaryIndex] attribute parsing and numbering
- **UnixTimestampAttributeHandlerTests**: Tests for [UnixTimestamp] attribute detection

### Code Generation Tests
- **MapperGeneratorTests**: Tests for ToDynamoRecord and FromDynamoRecord generation
- **KeyFactoryGeneratorTests**: Tests for key factory generation
- **CodeBuilderTests**: Tests for code formatting utilities
- **NamingHelpersTests**: Tests for naming utilities

### Integration Tests
- **NullAwarenessIntegrationTests**: End-to-end tests for nullable type handling
- **GSIIntegrationTests**: Tests for GSI attribute generation
- **InheritanceIntegrationTests**: Tests for inheritance chain scenarios
- **UserProfileIntegrationTests**: Tests for GSI-only types inheriting from base classes

### Main Generator Tests
- **DynamoMapperIncrementalGeneratorTests**: Tests for the main source generator orchestration

## Test Coverage Goals

- **Unit Tests**: Cover all public methods and edge cases
- **Integration Tests**: Cover realistic scenarios from TestConsole project
- **Error Handling**: Test diagnostic reporting and error cases
- **Edge Cases**: Test boundary conditions, null inputs, empty collections

## Key Test Scenarios

1. **Nullable Type Handling**: Verifies nullable vs non-nullable types generate correct code
2. **MissingAttributeException**: Ensures non-nullable types throw exceptions instead of using defaults
3. **Property Name Casing**: Tests PascalCase properties vs camelCase constructor parameters
4. **GSI Auto-numbering**: Tests automatic GSI_X_PK/GSI_X_SK generation
5. **Inheritance Chains**: Tests complex inheritance scenarios (BaseEntity, UserProfile, etc.)
6. **Type Handler Priority**: Tests handler registration and priority ordering
7. **Date/Time Formatting**: Tests correct ISO formats for DateOnly/TimeOnly

## Running Tests

```bash
dotnet test
```

## Dependencies

- **TUnit**: Test framework
- **Moq**: Mocking framework  
- **Microsoft.CodeAnalysis**: Roslyn symbol mocking
- **Goa.Clients.Dynamo.Generator**: Target project under test