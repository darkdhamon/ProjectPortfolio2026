# TODO

- Add out-of-scope ideas here for future follow-up.
- Split admin and public project response shapes.
- Add stronger project validation rules such as URL validation, date-range validation, and duplicate-tag normalization.
- Add HTTP-level integration tests for the project API in addition to the current repository and controller tests.
- Ensure uploaded project images match the main display ratio of `2047:768`, and if an upload does not match, provide a user-controlled crop/position workflow before publishing.
- Optimize the placeholder image assets for production so the fallback visuals keep their styling without carrying unnecessary bundle weight.
- Evaluate and refine the public site's long-term color palette in a separate issue from dark-theme consistency work.
- Refactor `projectportfolio2026.client/src/App.tsx` into maintainable frontend modules before additional UI work keeps expanding the current monolith. Tracked in issue #45.
- Design password reset and account recovery for local accounts without requiring email or SMS delivery. Tracked in issue #59.
- Consider showing the logged-in user's display name in the admin navigation or header once the MVP admin shell is stable.
- Add account lockout and related login hardening rules in a follow-up issue after the MVP authentication flow is in place.
