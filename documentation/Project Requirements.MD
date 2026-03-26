# Project Portfolio Requirements Document

## 1. Overview

A web-based project portfolio platform designed for software developers to showcase their work to recruiters. The system should be flexible enough to support single or multiple developers and allow customization of content and deployment.

---

## 2. Goals

* Provide a professional portfolio for recruiters
* Showcase projects, skills, and experience
* Allow easy content management for the admin
* Be deployable across different environments (Azure, self-hosted, etc.)

---

## 3. Target Users

* **Admin/User (Developer):** Manages content and portfolio
* **Recruiters/Visitors:** Browse projects, resume, and contact information

---

## 4. Core Features (MVP)

### 4.1 Homepage

* Developer introduction blurb
* Featured projects carousel

  * Auto-rotates every 5 seconds
  * Pauses on hover

### 4.2 Project Management

* Database-backed projects
* Fields:

  * Title
  * Primary image
  * Screenshot gallery (optional)
  * Short description (max 350 characters)
  * Long description (Markdown supported)
  * GitHub link (optional)
  * Demo link (optional)
* Behavior:

  * Hide buttons if links are not provided
  * If no long description exists, optionally pull from GitHub README

### 4.3 Project List Page

* Infinite scrolling
* Search functionality
* Filter by skill tags

### 4.4 Skills & Tagging

* Projects tagged with skills
* Search/filter by skill

### 4.5 Resume Page

* Options:

  * Upload PDF
  * Embed via cloud (OneDrive/Google Drive)
* Optional proxy download system to bypass firewall restrictions

### 4.6 Contact Page

* Display contact details (email, phone, etc.)
* Dynamic social media links

### 4.7 Admin Panel

* Accessible via visible tab
* Redirect to login if not authenticated
* Features:

  * Create/edit projects
  * Manage featured projects
  * Manage social links
  * Resume configuration
* Live preview pane
* Draft vs published states

### 4.8 Authentication & Security

* Login required for admin
* Windows Authentication (preferred)
* Optional:

  * Authenticator app (2FA)
  * Passkey login support

### 4.9 Database

* Primary: SQL Server
* Abstraction via repository pattern
* Future support for interchangeable DB (e.g., MongoDB)

### 4.10 Testing & CI/CD

* Unit tests required
* Minimum 70% code coverage
* Deployment fails if threshold not met

---

## 5. Architecture

### Backend

* .NET Core (C#)
* Web API
* Repository pattern
* Organized structure:

  * Services/

    * Interfaces/
    * ServiceModels/
    * Implementations/
  * Repositories/

    * Interfaces/
    * Implementations/

### Frontend

* React
* MVVM-style separation

### Deployment

* Primary: Azure
* Support for alternative hosting

### Setup Wizard

* First-run configuration:

  * Database setup
  * Connection strings
  * Initial admin setup

---

## 6. Nice-to-Have Features

### 6.1 Timeline

* Display:

  * Project timelines
  * Certifications
  * Education
  * Life milestones

### 6.2 About Developer Page

* Project count
* Employment history
* Certifications (including expired)

### 6.3 Blog/Content Management

* Optional blog system
* Hidden if no entries

### 6.4 Contact Form

* With reCAPTCHA
* Email integration (future)

### 6.5 Analytics

* Page views per project
* Search analytics (skills searched)

### 6.6 GitHub Integration (Optional)

* Display recent commits

### 6.7 Testimonials

* Optional testimonial display

### 6.8 Reference Management (Private)

* Store references securely
* Tag by skills/projects/employers
* Generate downloadable reference lists
* Sort by relevance and recency

### 6.9 Portfolio PDF Export

* Generate downloadable portfolio summary

---

## 7. UI/UX Considerations

* Responsive design
* Dark mode (default enabled)
* Clean navigation
* Dynamic visibility (hide unused sections)

---

## 8. Open Questions

* Single-tenant vs multi-tenant architecture?

  * Separate deployments per developer
  * Shared platform with multiple users

---

## 9. Future Considerations

* Multi-user support
* Role-based access control
* Extended analytics
* Enhanced integrations

---

## 10. Summary

This project portfolio aims to balance simplicity for MVP with extensibility for future growth, while showcasing modern full-stack development practices.
