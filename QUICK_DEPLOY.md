# Quick Deployment Reference - Authentication Migration

## TL;DR - 3 Command Steps

### Step 1: Verify Code is Deployed
```bash
git push origin main
```
Wait for Railway auto-deploy to complete (check dashboard)

### Step 2: Run Migration
```bash
npm install -g @railway/cli
railway login
railway link
railway run dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api
```

### Step 3: Set Environment Variables in Railway Dashboard

**iDiski API Service → Variables Tab:**

```
Jwt__SecretKey = [paste output from: openssl rand -base64 64]
Jwt__Issuer = https://api.izinjuli.vercel.app
Jwt__Audience = https://izinjuli.vercel.app
```

Then click **Redeploy** on API service.

---

## Verify Success

**Check these tables exist in PostgreSQL:**
- ✅ Users
- ✅ UserRoles
- ✅ UserTeams
- ✅ UserDivisions

**Check API logs show:**
```
Applying migration '20260615101737_AddAuthenticationSystem'.
Done.
```

**No errors = ✅ Deployment Complete!**

---

## If Something Goes Wrong

1. Check Railway **Deployments** → Latest → **Logs**
2. Look for `error` or `failed` messages
3. Refer to full guide: `RAILWAY_DEPLOYMENT.md`

---

## Files Pushed to GitHub (main branch)

- ✅ Domain entities (User, UserRole, UserTeam, UserDivision)
- ✅ Backend services (Password hashing, JWT generation, email)
- ✅ Database migration (creates 5 new tables)
- ✅ This deployment guide

Ready to deploy!
