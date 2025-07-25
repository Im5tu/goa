# Credential Providers

This document describes how GOA handles AWS credential discovery and authentication, including the default credential provider chain, configuration options, and how to add custom credentials.

## Overview

GOA uses a credential provider chain that automatically discovers AWS credentials from multiple sources in a specific order. This follows the same pattern as the official AWS SDK, ensuring compatibility and familiar behavior.

## Default Credential Provider Chain

The credential providers are tried in the following order (highest to lowest priority):

### 1. Environment Variables (Highest Priority)
- **AWS_ACCESS_KEY_ID**: The AWS access key ID
- **AWS_SECRET_ACCESS_KEY**: The AWS secret access key
- **AWS_SESSION_TOKEN**: Optional session token for temporary credentials

**Example:**
```bash
export AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
export AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
export AWS_SESSION_TOKEN=optional-session-token
```

### 2. AWS Config and Credentials Files
GOA reads from both standard AWS configuration files:

#### Credentials File (`~/.aws/credentials`)
Contains AWS credentials with simple profile names:
```ini
[default]
aws_access_key_id = AKIAIOSFODNN7EXAMPLE
aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

[myprofile]
aws_access_key_id = AKIAI44QH8DHBEXAMPLE
aws_secret_access_key = je7MtGbClwBF/2Zp9Utk/h3yCo8nvbEXAMPLEKEY
aws_session_token = temporary-token
```

#### Config File (`~/.aws/config`)
Contains configuration settings and can also contain credentials:
```ini
[default]
region = us-east-1

[profile myprofile]
region = us-west-2
aws_access_key_id = AKIAI44QH8DHBEXAMPLE
aws_secret_access_key = je7MtGbClwBF/2Zp9Utk/h3yCo8nvbEXAMPLEKEY
```

**Profile Selection:**
- Uses `AWS_PROFILE` environment variable if set
- Defaults to `default` profile if no profile specified
- Profile names in config file use `profile` prefix (except default)

**File Merging:**
- Both credentials and config files are read and merged together
- Credentials file takes precedence over config file for the same properties
- This allows storing credentials in one file and configuration in another

**Custom File Paths:**
You can override the default file locations:
```bash
export AWS_SHARED_CREDENTIALS_FILE=/custom/path/credentials
export AWS_CONFIG_FILE=/custom/path/config
```

### 3. EC2 Instance Profile (Lowest Priority)
For applications running on EC2 instances, credentials are automatically retrieved from the Instance Metadata Service (IMDS).

- Uses IMDSv2 with token-based authentication for security
- Automatically refreshes credentials before expiration
- No configuration required when running on EC2

## Caching Behavior

### Chain-Level Caching
- Credentials are cached at the **chain level** (not individual providers)
- **Cache TTL**: 15 minutes
- **Thread-safe**: Multiple concurrent requests are handled safely
- **Automatic invalidation**: Cache is cleared on authentication failures (401/403 responses)

### Cache Reset and Expiration-Based Refresh
The credential cache is automatically reset when:
- Authentication fails (HTTP 401 or 403 responses)
- You manually call the reset functionality
- Cache TTL expires (15 minutes)
- **Credentials expire soon (within 1 minute of expiration time)**

For credentials with expiration times (like EC2 instance profile credentials), the cache automatically refreshes 1 minute before the credentials expire, even if the cache TTL hasn't been reached. This ensures uninterrupted service availability.

The credential provider chain will then be re-evaluated to obtain fresh credentials.

## Configuration

### Service Registration

GOA automatically sets up the default credential provider chain when you register services:

```csharp
services.AddDynamoDb();
```

This registers:
1. Environment variable provider
2. Config/credentials file provider
3. EC2 instance profile provider

### Adding Static Credentials

For testing or specific use cases, you can add static credentials:

```csharp
services.AddStaticCredentials(
    accessKeyId: "AKIAIOSFODNN7EXAMPLE",
    secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    sessionToken: "optional-session-token" // Optional
);
```

**Important Notes:**
- Static credentials have the **highest priority** when added
- Never hardcode credentials in production code
- Use static credentials primarily for testing scenarios

### Using Static Credentials from Configuration

For better security, load static credentials from app configuration:

```csharp
// appsettings.json
{
  "AWS": {
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "SessionToken": "optional-session-token"
  }
}

// Program.cs
var awsConfig = builder.Configuration.GetSection("AWS");
services.AddStaticCredentials(
    awsConfig["AccessKeyId"]!,
    awsConfig["SecretAccessKey"]!,
    awsConfig["SessionToken"]
);
```
