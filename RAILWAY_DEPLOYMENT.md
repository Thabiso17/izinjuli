# Railway Deployment Steps - Authentication System Migration

## Overview

The authentication system is now ready to deploy. This guide walks through deploying the migration to your Railway PostgreSQL database.

**Commits deployed:** `a968bb3` on main branch

---

## Step 1: Access Railway Dashboard

1. Go to [https://railway.app](https://railway.app)
2. Log in with your GitHub account
3. Click on your **iDiski** project

---

## Step 2: Locate Your PostgreSQL Database Service

In the Railway dashboard:
1. Click on **Plugins** (left sidebar) OR look for **PostgreSQL** service
2. Note the database name, typically: `idiski_db` or similar
3. You should see a **PostgreSQL** card in your services

---

## Step 3: Get Database Connection String from Railway

1. Click on the **PostgreSQL** service card
2. Click the **Connect** tab
3. Copy the connection string that looks like:
   ```
   postgresql://postgres:password@containers.railway.app:port/database
   ```
4. **Keep this somewhere safe** - you'll need it

**OR** if using Railway's default `DATABASE_URL`:
- It's automatically set as an environment variable
- The app's `Program.cs` already handles conversion

---

## Step 4: Deploy Code to Railway (Git Push)

1. Go back to your local terminal
2. Verify you're on main branch:
   ```bash
   git branch
   ```
3. Push the latest commits (already done):
   ```bash
   git push origin main
   ```
4. Railway auto-deploys on push (if configured)
5. **Wait for deployment to complete** (check Railway dashboard for build status)

---

## Step 5: Run Database Migration via Railway CLI

### Option A: Using Railway CLI (Recommended)

1. **Install Railway CLI** (if not already installed):
   ```bash
   npm install -g @railway/cli
   ```

2. **Login to Railway**:
   ```bash
   railway login
   ```
   (Opens browser to authenticate with GitHub)

3. **Navigate to your project directory**:
   ```bash
   cd C:\Users\CP378669\source\repos\diski
   ```

4. **Link to Railway project**:
   ```bash
   railway link
   ```
   (Select your iDiski project from the menu)

5. **Run the migration**:
   ```bash
   railway run dotnet ef database update \
     --project iDiski.Infrastructure \
     --startup-project iDiski.Api
   ```

   **Expected output:**
   ```
   Build started...
   Build succeeded.
   Applying migration '20260615101737_AddAuthenticationSystem'.
   Done.
   ```

---

### Option B: Using Railway Dashboard Shell (Alternative)

1. In Railway dashboard, go to your **iDiski API** service
2. Click **Terminal** tab
3. Run the migration command:
   ```bash
   dotnet ef database update \
     --project iDiski.Infrastructure \
     --startup-project iDiski.Api
   ```

---

## Step 6: Verify Migration Succeeded

After migration completes, verify the tables were created:

### Option A: Using Railway CLI

```bash
railway run psql << EOF
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;
EOF
```

**Look for these new tables:**
- ✅ `"Users"`
- ✅ `"UserRoles"`
- ✅ `"UserTeams"`
- ✅ `"UserDivisions"`
- ✅ `"ArticleAttachments"`

### Option B: Using Railway Dashboard

1. Click **PostgreSQL** service
2. Click **Data** tab
3. Expand the database in the tree
4. Verify tables exist under **Tables**

---

## Step 7: Configure JWT Environment Variables in Railway

1. In Railway dashboard, click your **iDiski API** service
2. Click **Variables** tab
3. Add these environment variables:

| Variable | Value | Notes |
|----------|-------|-------|
| `Jwt__SecretKey` | `[64-char random]` | Generate with: `openssl rand -base64 64` |
| `Jwt__Issuer` | `https://api.izinjuli.vercel.app` | Your API domain |
| `Jwt__Audience` | `https://izinjuli.vercel.app` | Your frontend domain |

**To generate secret key** (run in terminal):
```bash
openssl rand -base64 64
```

Copy the output and paste into `Jwt__SecretKey` field.

---

## Step 8: Configure Email Environment Variables (Optional, for Password Reset)

If you want to enable password reset via email, add:

| Variable | Value | Example |
|----------|-------|---------|
| `Email__From` | Email sender address | `noreply@izinjuli.com` |
| `Email__SmtpHost` | SMTP server | `smtp.gmail.com` |
| `Email__SmtpPort` | Port | `587` |
| `Email__SmtpUser` | Email account | `your-email@gmail.com` |
| `Email__SmtpPassword` | App password | From Gmail App Passwords |

**For Gmail:**
1. Enable 2-factor authentication on your Google account
2. Go to https://myaccount.google.com/apppasswords
3. Generate an app-specific password
4. Use that in `Email__SmtpPassword`

---

## Step 9: Restart the API Service

1. In Railway dashboard, click your **iDiski API** service
2. Click **Deploy** or **Redeploy** button
3. Wait for new build to complete
4. Check logs for any errors

---

## Step 10: Verify API Can Connect to Database

1. Go to your API's **Deployments** tab
2. Click the latest deployment
3. View the **Logs** tab
4. Look for messages like:
   ```
   Database connection configured.
   Applying migration...
   Done.
   ```
5. No errors = ✅ Success!

---

## Troubleshooting

### Migration Shows "Build failed"

**Cause:** Environment variables not set correctly

**Solution:**
1. Check `DATABASE_URL` is set in Railway PostgreSQL service
2. Verify API service has access to it
3. Run migration again

### "Password authentication failed"

**Cause:** Database credentials wrong

**Solution:**
1. Verify `DATABASE_URL` in Railway PostgreSQL plugin
2. Copy the correct connection string
3. Make sure API service has `DATABASE_URL` environment variable

### "Table already exists"

**Cause:** Migration was already applied

**Solution:**
1. This is safe - no action needed
2. Verify all 5 new tables exist in database
3. If some tables missing, check migration was fully applied

### API Won't Start After Migration

**Cause:** Environment variables missing

**Solution:**
1. Verify `Jwt__SecretKey` is set (minimum 64 characters)
2. Verify `Jwt__Issuer` and `Jwt__Audience` are set
3. Redeploy API service
4. Check logs again

---

## Full Command Reference

```bash
# 1. Install Railway CLI
npm install -g @railway/cli

# 2. Login
railway login

# 3. Navigate to project
cd C:\Users\CP378669\source\repos\diski

# 4. Link to Railway project
railway link

# 5. Deploy code (automatic with git push, or manual redeploy in dashboard)
git push origin main

# 6. Run migration
railway run dotnet ef database update \
  --project iDiski.Infrastructure \
  --startup-project iDiski.Api

# 7. Verify tables
railway run psql -c "SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name;"

# 8. Check migration history
railway run dotnet ef migrations list \
  --project iDiski.Infrastructure \
  --startup-project iDiski.Api
```

---

## After Deployment Complete ✅

Once migration is applied and verified:

1. **Next task:** Create LoginCommand and AuthenticationController
2. **Then:** Update Program.cs with JWT middleware configuration
3. **Then:** Build Angular login component
4. **Finally:** Test end-to-end authentication flow

See `DEPLOYMENT_NOTES.md` for detailed technical documentation.

---

## Getting Help

**If deployment fails:**

1. Check Railway logs (Deployments → Latest → Logs)
2. Verify all environment variables are set
3. Verify database connection string is correct
4. Try running migration locally first (requires local PostgreSQL)

**Contact:**
- Railway docs: https://docs.railway.app
- Entity Framework Core: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
