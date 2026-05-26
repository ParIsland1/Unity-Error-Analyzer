# Unity Error Analyzer

Unity Error Analyzer is an AI-powered debugging assistant for Unity developers. It lets a user paste a Unity console error, describe what they were doing, and optionally include a C# script snippet. The app then uses the OpenAI API to return a structured debugging explanation with likely causes, fix steps, code suggestions, and Unity Inspector checks.

This project was built as a portfolio project to practice building real AI API tools using C#, ASP.NET Core, frontend JavaScript, and prompt engineering.

## Features

- Paste Unity console errors for analysis
- Add context about what was happening in the Unity project
- Include relevant C# code snippets
- Get structured debugging output:
  - Likely cause
  - Why the error happens
  - Fix steps
  - Code fix suggestions
  - Unity Inspector checks
  - Confidence level
- Uses a backend API so the OpenAI API key is not exposed in the browser
- Includes basic input validation and API error handling

## Tech Stack

- C#
- ASP.NET Core Minimal API
- HTML
- CSS
- JavaScript
- OpenAI API

## Why I Built This

As a Unity developer, I frequently debug C# scripts, scene reference issues, Inspector assignment problems, and runtime errors. I built this tool to explore how AI can be used as a practical developer assistant instead of just a chatbot.

The goal was to learn how to:

- Build an AI-powered tool using an API
- Create a simple C# backend
- Send user input from a frontend to a backend
- Safely use an API key through environment variables
- Design a reusable prompt for a specific developer workflow
- Return structured and useful AI output

## How It Works

The app follows this flow:

```text
User enters Unity error, context, and code snippet
        ↓
Frontend sends a POST request to /api/analyze
        ↓
ASP.NET Core backend validates the input
        ↓
Backend sends a structured prompt to the OpenAI API
        ↓
AI response is returned to the backend
        ↓
Frontend displays the analysis to the user
