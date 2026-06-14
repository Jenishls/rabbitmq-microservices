name: Fly Deploy

on:
  push:
    branches:
      - main # Triggers the deployment every time you push to main

jobs:
  deploy:
    name: Deploy App to Fly.io
    runs-on: ubuntu-latest
    concurrency: deploy-group    # Prevents simultaneous deployments from colliding
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Set up Flyctl
        uses: superfly/flyctl-actions/setup-flyctl@master

      - name: Deploy to Fly.io
        run: flyctl deploy --remote-only
