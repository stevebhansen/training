# Training Monorepo

This is a monorepo containing both the frontend (Angular) and backend (.NET) applications.

## Project Structure

```
├── clientapp/            # Angular frontend application
├── backend/              # .NET backend API
├── package.json          # Root package.json for monorepo management
└── README.md             # This file
```

## Development Setup

### Prerequisites

- Node.js (v18+)
- npm (v9+)
- .NET SDK (v8.0+)

### Installation

1. Install root dependencies:

   ```
   npm install
   ```

2. Install client dependencies:
   ```
   npm run install:clientapp
   ```

### Running the Applications

Start both frontend and backend together:

```
npm start
```

Or run them individually:

```
npm run start:backend    # Starts the .NET API on http://localhost:5296
npm run start:clientapp  # Starts the Angular app on http://localhost:4200
```

## Building for Production

Build both applications:

```
npm run build
```

This will:

1. Build the .NET backend
2. Build the Angular frontend

## Testing

Run all tests:

```
npm test
```

Run tests for individual projects:

```
npm run test:backend
npm run test:clientapp
```

## Development Workflow

This monorepo uses a single Git repository to manage both projects. When working on features that span both frontend and backend, you can make changes to both codebases and commit them together.
