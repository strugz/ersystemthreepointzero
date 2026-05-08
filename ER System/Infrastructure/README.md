# Infrastructure layer

External integrations belong here.

Use this layer for SQL Server repositories, registry/app.config providers, email sending, Crystal Reports/PDF/export adapters, logging, and file-system integration. New database code should use parameters and dispose connections/commands/readers with `Using` blocks.
