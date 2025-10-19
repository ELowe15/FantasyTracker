# Fantasy Keeper – Project Context

## Overview
Fantasy Keeper is a fantasy sports web application.  
It runs entirely on **GitHub Pages** as a static site, with **GitHub Actions** used to pull and update data from external fantasy APIs.  
There is **no live backend server** — instead, a .NET job runs during builds to fetch and prepare data, which is then consumed by the static frontend.

---

## Hosting & Deployment
- **Platform**: GitHub Pages (static hosting only).
- **Build Automation**: GitHub Actions.
  - Executes a .NET project written in **C#**.
  - The C# code connects to fantasy sports APIs (e.g., Yahoo Fantasy), fetches and transforms the data, and outputs JSON or other static assets into the repo.
  - These assets are then deployed as part of the GitHub Pages site.

---

## Architecture
- **Frontend**:
  - Purely static, built with either **JavaScript** or **TypeScript** (to be decided).
  - Loads pre-fetched JSON data generated during the build.
  - Handles rendering, interactivity, and presentation in the browser.

- **Build-time "pseudo backend"**:
  - Implemented in **.NET + C#**.
  - Runs inside GitHub Actions on a schedule or when triggered.
  - Responsible for:
    - Fetching data from external APIs.
    - Normalizing and transforming data into static JSON.
    - Saving results into `/data/` or another designated directory for the frontend.

- **Data flow**:
  - GitHub Actions runs → executes C# build job → stores JSON data → static frontend loads JSON.

---

## Technology Stack
- **Frontend**: Static HTML + CSS + (JavaScript | TypeScript).
- **Build scripts**: .NET 8 (C#).
- **Automation**: GitHub Actions.
- **Data storage**: Static JSON files committed to the repo.

---

## Key Constraints
- Must remain fully static on GitHub Pages — no runtime server execution.
- All API calls are performed at **build-time** by the C# code.
- The browser never makes direct authenticated API calls.
- No user authentication or data submission is currently planned.
- CSS should be modular and organized across multiple files.

---

## Current Decisions
- **Frontend** will be static with JS or TS (TBD).
- **Data pipeline**:
  - C#/.NET job runs in GitHub Actions.
  - Outputs JSON for the frontend.
- **Context management**:
  - This file (`docs/CONTEXT.md`) is the authoritative source of project requirements and decisions.
  - Each source file should include a short header comment describing its role.

---

## Open Questions
- Final choice between **JavaScript** and **TypeScript** for the frontend.
- Which external APIs to integrate beyond Yahoo Fantasy (if any).
- Final UI/UX layout and feature set.

---