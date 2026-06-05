# ER System 3.0 Release Documentation Plan

## Purpose

This plan defines the documentation that should be prepared and reviewed for each ER System 3.0 release. The goal is to keep release materials useful for deployment owners, support staff, administrators, finance reviewers, approvers, and regular users.

## Documentation Set

Maintain these release documents:

- `docs/release-docs/deployment-guide.md`
  - Deployment prerequisites, database migration order, build procedure, smoke testing, and rollback.
- `docs/release-docs/user-manual.md`
  - Role-based user guidance for User, Admin, and Finance workflows.
- Release notes for each production release.
  - New features, fixes, known issues, database scripts, and verification notes.
- Support checklist.
  - Common login, connection, report, receipt, approval, and finance review issues.

## Audience

### Deployment Owner

Needs:

- Build and installer steps.
- Database migration order.
- Environment prerequisites.
- Smoke test checklist.
- Rollback procedure.

Primary document:

- Deployment Guide

### Administrator

Needs:

- User account setup.
- Role assignment.
- Department and signatory setup.
- Email setup.
- Connection setup basics.
- Approval configuration.

Primary document:

- User Manual, Admin section

### Finance

Needs:

- Finance Review queue usage.
- ERF filtering.
- Physical receipt tracking.
- SMS notification workflow.
- Handling missing scanned or physical receipts.

Primary document:

- User Manual, Finance section

### Regular User

Needs:

- Login.
- Account settings.
- Creating and updating ERFs.
- Adding expenses.
- Filing reports.
- Submitting scanned and physical receipts.
- Viewing previous ERs.

Primary document:

- User Manual, User section

### Support Staff

Needs:

- Known deployment risks.
- Common user problems.
- Role visibility rules.
- Basic troubleshooting.

Primary documents:

- Deployment Guide
- User Manual
- Release notes

## Documentation Workflow

1. Review code and database changes for the release.
2. Identify changed forms, services, database tables, scripts, and installer behavior.
3. Update the Deployment Guide when prerequisites, scripts, installer steps, smoke tests, or rollback steps change.
4. Update the User Manual when visible UI, role behavior, workflow steps, messages, or required user actions change.
5. Prepare release notes using the release notes template.
6. Review documentation with one technical reviewer and one business workflow reviewer.
7. Attach documentation to the deployment package or release handoff.

## Release Notes Template

```text
Release name:
Release date:
Prepared by:

Summary:

New features:

Fixes:

Database changes:

Installer changes:

User workflow changes:

Admin workflow changes:

Finance workflow changes:

Known issues:

Verification performed:

Rollback notes:
```

## Content Ownership

- Deployment Guide: release/deployment owner.
- User Manual: product owner or business process owner, with developer support.
- Release notes: developer preparing the release.
- Support checklist: support lead, updated from production issues.

## Review Checklist

Before documentation is approved, confirm:

- Document names and paths are current.
- Database scripts are listed in the correct order.
- No production passwords or secrets are included.
- Role names match the application, especially `Admin`, `User`, and `Finance`.
- Menu names match the visible UI.
- New workflow fields are documented.
- Known issues and limitations are clear.
- Smoke tests match the release scope.
- Rollback instructions are practical for the deployed package.

## Change Triggers

Update release documentation whenever any of these change:

- Database schema or migration scripts.
- Installer project or package output.
- Application prerequisites.
- Connection or registry behavior.
- Login behavior.
- User level or menu visibility behavior.
- Expense report fields.
- Approval workflow.
- Finance Review behavior.
- SMS notification behavior.
- Report printing or Crystal Reports dependencies.

## Suggested Future Additions

- Screenshot-based quick start guide.
- One-page finance checklist for physical receipt processing.
- Troubleshooting guide for connection setup and Crystal Reports runtime issues.
- UAT script covering User, Admin, Finance, and Approver scenarios.
- Release acceptance checklist signed by deployment owner and business owner.
