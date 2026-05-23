# Quick Deployment Guide

## 🚀 Deploy to Railway.app (Backend + Database)

### Step 1: Create Railway Account
1. Go to https://railway.app
2. Click **"Login"** → **"Login with GitHub"**
3. Authorize Railway

### Step 2: Deploy Backend
1. Click **"Start a New Project"**
2. Select **"Deploy from GitHub repo"**
3. Choose **"izinjuli"** repository (or your repo name)
4. Railway will start building automatically

### Step 3: Add Database
1. In your project, click **"+ New"** → **"Database"** → **"Add PostgreSQL"**
2. Database will be provisioned automatically
3. `DATABASE_URL` is automatically linked to your service

### Step 4: Generate Public URL
1. Click on your API service (not database)
2. Go to **"Settings"** → **"Networking"**
3. Click **"Generate Domain"**
4. Copy the URL (e.g., `https://idiski-production.up.railway.app`)

### Step 5: Set Environment Variables
1. Click on your API service
2. Go to **"Variables"** tab
3. Click **"+ New Variable"**
4. Add:
   - Key: `ASPNETCORE_ENVIRONMENT`, Value: `Production`
   - Key: `ProductionOrigin`, Value: `https://idiski.vercel.app` (update after Vercel deployment)

### Step 6: Run Database Migrations
After deployment, apply migrations by calling:
```bash
curl -X POST https://your-railway-url.up.railway.app/api/migrate
```

Or use the manual SQL endpoint if needed:
```bash
curl -X POST https://your-railway-url.up.railway.app/api/migrate/manual
```

### Step 7: Seed Database
Once deployed, visit:
```
https://your-railway-url.up.railway.app/api/seed/comprehensive
```

This will populate your database with sample data.

---

## 🌐 Deploy to Vercel (Frontend)

### Step 1: Create Vercel Account
1. Go to https://vercel.com
2. Click **"Sign Up"** → **"Continue with GitHub"**
3. Authorize Vercel

### Step 2: Import Project
1. Click **"Add New..."** → **"Project"**
2. Click **"Import"** next to your **izinjuli** repository
3. Configure:
   - **Framework Preset:** Vite (or Other if Vite not available)
   - **Root Directory:** Click **"Edit"** → Select `iDiski-Client`
   - **Build Command:** `npm run build`
   - **Output Directory:** `dist/idisi-client/browser`

4. Click **"Deploy"**

### Step 3: Copy Vercel URL
After deployment completes, copy your Vercel URL (e.g., `https://idiski.vercel.app`)

### Step 4: Update Railway CORS
1. Go back to Railway
2. Click on your API service → **"Variables"**
3. Edit `ProductionOrigin` and paste your Vercel URL
4. Railway will automatically redeploy

---

## ✅ Test Your Deployment

### Backend Tests:
- Health: `https://your-railway-url.up.railway.app/api/health`
- Teams: `https://your-railway-url.up.railway.app/api/teams`
- Swagger: `https://your-railway-url.up.railway.app/openapi/v1.json`

### Frontend Tests:
- Home: `https://your-vercel-url.vercel.app`
- Teams: `https://your-vercel-url.vercel.app/teams`
- Admin: `https://your-vercel-url.vercel.app/admin/players`

---

## 🔧 Troubleshooting

### Backend not loading data?
Visit the seed endpoint: `https://your-railway-url/api/seed/comprehensive`

### Frontend showing CORS errors?
1. Check that `ProductionOrigin` in Railway matches your Vercel URL exactly
2. Make sure it's HTTPS (not HTTP)
3. Railway will redeploy when you update the variable

### 404 errors on frontend routes?
The `vercel.json` file should handle this. If issues persist:
1. Check Vercel build logs
2. Verify output directory is correct: `dist/idisi-client/browser`

### Railway build failing?
1. Check build logs in Railway dashboard
2. Verify `railway.json` and `nixpacks.toml` are in repository root
3. Ensure .NET 9 is being used

---

## 📝 Your URLs

After deployment, save these for reference:

```
Backend API: https://______________.up.railway.app
Frontend:    https://______________.vercel.app
Database:    (managed by Railway, accessible only via API)
```

---

## 🔄 Future Updates

To deploy changes:
1. Make changes locally
2. Commit: `git add . && git commit -m "Your message"`
3. Push: `git push origin main`
4. Railway and Vercel will **automatically** rebuild and deploy! 🎉

**Database Schema Changes:**
When you add new migrations (e.g., new tables, columns):
1. Create migration: `dotnet ef migrations add YourMigrationName --project iDiski.Infrastructure --startup-project iDiski.Api`
2. Commit and push to trigger deployment
3. Run migration endpoint: `curl -X POST https://your-railway-url/api/migrate`

---

## 💰 Cost

Both services are **completely FREE** for your usage:
- Railway: 500 hours/month, 500MB PostgreSQL
- Vercel: Unlimited for personal projects

No credit card required! ✨
