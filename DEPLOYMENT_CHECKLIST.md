# Railway Deployment Checklist

## Initial Setup (One-time)

### 1. Create Cloudinary Account
- [ ] Sign up at https://cloudinary.com/users/register/free
- [ ] Copy Cloud Name, API Key, and API Secret from dashboard

### 2. Configure Railway Environment Variables

Add these in Railway project → Settings → Variables:

```bash
# Database (automatically provided by Railway)
DATABASE_URL=<provided by Railway Postgres>

# Cloudinary file storage
USE_CLOUDINARY=true
Cloudinary__CloudName=your-cloud-name-here
Cloudinary__ApiKey=your-api-key-here
Cloudinary__ApiSecret=your-api-secret-here

# Frontend origin (for CORS)
ProductionOrigin=https://izinjuli.vercel.app
```

### 3. Deploy Backend

```bash
# From project root
git add .
git commit -m "feat: Add Cloudinary file storage"
git push
```

Railway will automatically detect changes and redeploy.

### 4. Run Database Migration

After first deployment, hit the migration endpoint:
```bash
POST https://izinjuli-production.up.railway.app/api/migrate
```

Or use curl:
```bash
curl -X POST https://izinjuli-production.up.railway.app/api/migrate
```

### 5. Seed Database (Optional)

Seed with comprehensive historical data:
```bash
GET https://izinjuli-production.up.railway.app/api/seed/comprehensive
```

## Verifying Deployment

### Backend Health Check
```bash
GET https://izinjuli-production.up.railway.app/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-05-27T12:00:00Z"
}
```

### File Upload Test

1. Open Angular app at https://izinjuli.vercel.app
2. Go to Admin → Players → Create New Player
3. Upload a player image
4. Check that the image URL starts with `https://res.cloudinary.com/`
5. Verify image displays correctly

### Check Logs

In Railway dashboard:
- Look for: `"Using Cloudinary for file storage"` in startup logs
- No errors about missing Cloudinary credentials

## Common Issues

### Issue: Images return 404

**Symptom**: `GET /uploads/players/abc.jpg 404 (Not Found)`

**Solution**: 
- Ensure `USE_CLOUDINARY=true` in Railway
- Verify all three Cloudinary credentials are set
- Redeploy if needed

### Issue: "Cloudinary:CloudName not configured"

**Solution**: Add missing environment variables with `__` syntax:
- `Cloudinary__CloudName` (note the double underscore)
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`

### Issue: Old images still show Railway URLs

**Solution**: Old images in database have `/uploads/...` URLs. Re-upload them after Cloudinary is configured.

## File Storage Behavior

| Environment | Storage Backend | Configuration Required |
|------------|----------------|----------------------|
| **Local Development** | `wwwroot/uploads` | None (default) |
| **Railway Production** | Cloudinary | Set `USE_CLOUDINARY=true` + credentials |

## Rollback Plan

If Cloudinary isn't working:

1. Remove `USE_CLOUDINARY` from Railway (or set to `false`)
2. Redeploy
3. Note: Images will be lost on container restart (ephemeral storage)

For permanent solution, must use cloud storage like Cloudinary.

## Monitoring

Check Cloudinary usage:
- Dashboard: https://cloudinary.com/console
- Storage: Track against 25 GB limit
- Bandwidth: Track against 25 GB/month limit
- Transformations: Track against 25k/month limit

## Next Steps

- [ ] Set up automated backups of PostgreSQL database
- [ ] Configure custom domain (optional)
- [ ] Set up monitoring/alerts (Railway provides basic metrics)
- [ ] Review and optimize Cloudinary transformations for performance
