# TODO

- Add out-of-scope ideas here for future follow-up.
- Add delete support for projects.
- Add search, filter, and query-specific repository methods for the future project list page.
- Split admin and public project response shapes.
- Add stronger project validation rules such as URL validation, date-range validation, and duplicate-tag normalization.
- Add HTTP-level integration tests for the project API in addition to the current repository and controller tests.
- Restrict unpublished project detail access for general users while preserving admin visibility once authentication and admin APIs are in place. The public `GET /api/projects/{id}` path should not expose draft or unpublished projects to anonymous users.
