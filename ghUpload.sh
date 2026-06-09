# Replace with your repo name
REPO_NAME="R2SentinelBak"

# Create private repo and add remote
gh repo create $REPO_NAME --private --source=. --remote=origin

# Push main branch
git checkout main
git push -u origin main

# Push develop
git push -u origin develop

# Push feature branch
git push -u origin feature/initial-setup