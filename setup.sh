#!/bin/bash

# Exit on error
set -e

# Init repo and set main branch
git init
git branch -M main

# Create README
echo "# R2SentinelBak" > README.md

# GitHub Actions structure
mkdir -p .github/workflows

# Build workflow
cat <<EOF > .github/workflows/build.yml
name: Build

on:
  push:
    branches: [develop, main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - run: echo "Build OK"
EOF

# Deploy workflow
cat <<EOF > .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - run: echo "Deploy OK"
EOF

# Docker compose
cat <<EOF > docker-compose.yml
version: '3.9'

services:
  app:
    image: alpine
    command: ["echo", "Hello"]
EOF

# Initial commit
git add .
git commit -m "Initial commit"

# Branching
git checkout -b develop
git checkout -b feature/initial-setup

echo "Local setup done."