# Presentation layer

WinForms UI belongs here as the application is modernized.

Current guidance:

- Keep existing forms in their current locations until each move is verified in Visual Studio.
- New UI orchestration should prefer presenter classes or small view models instead of adding workflow logic directly to forms.
- Forms should handle control events, visual state, and calls into application services.
