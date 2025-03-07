#!/usr/bin/env node

/**
 * Script to build and prepare the monorepo for deployment
 */

const { execSync } = require("child_process");
const fs = require("fs");
const path = require("path");

// Set up the deploy directory
const deployDir = path.join(__dirname, "deploy");
const clientDistDir = path.join(
  __dirname,
  "clientapp",
  "dist",
  "clientapp",
  "browser"
);
const backendPublishDir = path.join(
  __dirname,
  "backend",
  "bin",
  "Release",
  "net8.0",
  "publish"
);

// Ensure deploy directory exists
if (!fs.existsSync(deployDir)) {
  fs.mkdirSync(deployDir, { recursive: true });
}

console.log("🚀 Starting monorepo build process...");

try {
  // Build backend
  console.log("\n📦 Building and publishing .NET backend...");
  execSync("dotnet publish -c Release", {
    cwd: path.join(__dirname, "backend"),
    stdio: "inherit",
  });

  // Build frontend
  console.log("\n📦 Building Angular frontend...");
  execSync("npm run build -- --configuration=production", {
    cwd: path.join(__dirname, "clientapp"),
    stdio: "inherit",
  });

  // Copy backend files to deploy directory
  console.log("\n📋 Copying backend files to deploy directory...");
  execSync(`cp -r ${backendPublishDir}/* ${deployDir}/`);

  // Create wwwroot if it doesn't exist
  const wwwrootDir = path.join(deployDir, "wwwroot");
  if (!fs.existsSync(wwwrootDir)) {
    fs.mkdirSync(wwwrootDir, { recursive: true });
  }

  // Copy frontend files to wwwroot
  console.log("\n📋 Copying frontend files to deploy directory...");
  execSync(`cp -r ${clientDistDir}/* ${wwwrootDir}/`);

  console.log(
    '\n✅ Build complete! Deployment files are in the "deploy" directory.'
  );
} catch (error) {
  console.error("\n❌ Build failed:", error);
  process.exit(1);
}
