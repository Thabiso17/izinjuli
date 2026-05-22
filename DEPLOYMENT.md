# iDiski Deployment Guide

This guide will help you deploy the iDiski application to Railway.app (backend + database) and Vercel (frontend).

## Prerequisites

- GitHub account
- Railway.app account (sign up at https://railway.app - free, no credit card required)
- Vercel account (sign up at https://vercel.com - free)
- Git installed locally

## Part 1: Push Code to GitHub

1. **Initialize Git repository (if not already done):**
   ```bash
   cd C:\Users\CP378669\source\repos\diski
   git add .
   git commit -m "Prepare for Railway and Vercel deployment"
   git push origin main
   ```

## Part 2: Deploy Backend to Railway.app

### Step 1: Create Railway Project

1. Go to https://railway.app
2. Click **"Start a New Project"**
3. Select **"Deploy from GitHub repo"**
4. Authorize Railway to access your GitHub repositories
5. Select your **`diski`** repository
6. Railway will automatically detect the .NET application

### Step 2: Add PostgreSQL Database

1. In your Railway project dashboard, click **"+ New"**
2. Select **"Database"** → **"Add PostgreSQL"**
3. Railway will automatically provision a PostgreSQL database
4. The connection string will be available as `DATABASE_URL` environment variable

### Step 3: Configure Environment Variables

1. Click on your **API service** (not the database)
2. Go to **"Variables"** tab
3. Add the following variables:

   ```
   ASPNETCORE_ENVIRONMENT=Production
   ProductionOrigin=https://idiski.vercel.app
   ```

   **Note:** Replace `https://idiski.vercel.app` with your actual Vercel URL after frontend deployment

### Step 4: Configure Build Settings

Railway should automatically use the `nixpacks.toml` and `railway.json` files. If needed:

1. Go to **"Settings"** tab
2. Under **"Build Command"**, it should show:
   ```
   dotnet publish iDiski.Api/iDiski.Api.csproj -c Release -o out
   ```
3. Under **"Start Command"**, it should show:
   ```
   cd out && dotnet iDiski.Api.dll
   ```

### Step 5: Run Database Migrations

Once the API is deployed and running:

1. Go to your Railway API service
2. Click **"Settings"** → **"Generate Domain"** to get a public URL
3. Your API will be available at: `https://idiski-production.up.railway.app`
4. Visit the seed endpoint to initialize the database:
   ```
   GET https://idiski-production.up.railway.app/api/seed/comprehensive
   ```

### Step 6: Verify Backend Deployment

Test these endpoints:
- Health check: `https://idiski-production.up.railway.app/api/health`
- Get teams: `https://idiski-production.up.railway.app/api/teams`

## Part 3: Deploy Frontend to Vercel

### Step 1: Install Vercel CLI (Optional)

You can deploy via Vercel dashboard or CLI. For CLI:

```bash
npm install -g vercel
```

### Step 2: Deploy via Vercel Dashboard (Recommended)

1. Go to https://vercel.com/dashboard
2. Click **"Add New..."** → **"Project"**
3. Import your GitHub repository (`diski`)
4. Configure project:
   - **Framework Preset:** Angular
   - **Root Directory:** `iDiski-Client`
   - **Build Command:** `npm run build`
   - **Output Directory:** `dist/idisi-client/browser`
   - **Install Command:** `npm install`

5. Click **"Deploy"**

### Step 3: Update CORS in Backend

After frontend deployment, you'll get a Vercel URL like: `https://idiski-abc123.vercel.app`

1. Go back to Railway
2. Update the `ProductionOrigin` environment variable with your Vercel URL
3. Railway will automatically redeploy

### Step 4: Verify Frontend Deployment

1. Visit your Vercel URL
2. Navigate to different pages (Teams, Players, Matches)
3. Check browser console for any errors
4. Test admin functionality at: `https://your-vercel-url.vercel.app/admin/players`

## Part 4: Custom Domain (Optional)

### For Railway (Backend):
1. In Railway project settings
2. Go to **"Settings"** → **"Networking"**
3. Add your custom domain (e.g., `api.idiski.com`)

### For Vercel (Frontend):
1. In Vercel project settings
2. Go to **"Settings"** → **"Domains"**
3. Add your custom domain (e.g., `www.idiski.com`)

## Deployment URLs

After deployment, your application will be available at:

- **Frontend (Public Site):** `https://idiski.vercel.app`
- **Backend API:** `https://idiski-production.up.railway.app`
- **API Health Check:** `https://idiski-production.up.railway.app/api/health`
- **Swagger Docs:** `https://idiski-production.up.railway.app/openapi/v1.json`

## Troubleshooting

### Backend Issues

**Database Connection Error:**
- Check that PostgreSQL is running in Railway
- Verify `DATABASE_URL` environment variable is set
- Check Railway logs: Click on service → "Logs" tab

**Build Failures:**
- Ensure `railway.json` and `nixpacks.toml` are in the root directory
- Check Railway build logs
- Verify .NET 9 SDK is being used

**CORS Errors:**
- Verify `ProductionOrigin` matches your Vercel URL exactly
- Check that HTTPS is used (not HTTP)

### Frontend Issues

**API Calls Failing:**
- Check `environment.production.ts` has correct Railway URL
- Verify CORS is configured correctly in backend
- Check browser console for specific error messages

**Routing Issues (404 on refresh):**
- Verify `vercel.json` has correct rewrites configuration
- Check output directory matches build output

**Build Failures:**
- Run `npm install` locally to verify dependencies
- Check Node.js version compatibility
- Verify `angular.json` build configuration

## Continuous Deployment

Both Railway and Vercel support automatic deployments:

- **Push to `main` branch** → Both services automatically rebuild and deploy
- **Pull Requests** → Vercel creates preview deployments
- **Monitor deployments** in respective dashboards

## Cost & Limits

### Railway (Free Tier):
- 500 hours/month execution time
- 500MB PostgreSQL storage
- 1GB RAM
- Should be sufficient for testing and small-scale production

### Vercel (Free Tier):
- Unlimited bandwidth for personal projects
- 100GB bandwidth for commercial
- Automatic HTTPS
- Global CDN

## Support

If you encounter issues:
- Railway docs: https://docs.railway.app
- Vercel docs: https://vercel.com/docs
- Check application logs in respective dashboards
