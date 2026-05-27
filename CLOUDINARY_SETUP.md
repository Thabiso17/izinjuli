# Cloudinary Setup Guide

## Why Cloudinary?

Railway (and similar platforms) use **ephemeral file systems** - any files uploaded to the server are deleted when the container restarts. To have persistent image storage, you need a cloud storage solution like Cloudinary.

## Setup Steps

### 1. Create a Cloudinary Account

1. Go to [https://cloudinary.com/users/register/free](https://cloudinary.com/users/register/free)
2. Sign up for a free account (25 GB storage, 25 GB bandwidth/month)
3. After registration, go to your Dashboard

### 2. Get Your Credentials

From the Cloudinary Dashboard, copy:
- **Cloud Name** (e.g., `dzabcdef123`)
- **API Key** (e.g., `123456789012345`)
- **API Secret** (e.g., `abcdefghijklmnopqrstuvwxyz123456`)

### 3. Configure Railway Environment Variables

In your Railway project settings, add these environment variables:

```bash
# Enable Cloudinary
USE_CLOUDINARY=true

# Cloudinary credentials
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret
```

**Note**: Railway uses double underscores `__` to represent nested configuration (e.g., `Cloudinary__CloudName` maps to `Cloudinary:CloudName` in appsettings.json).

### 4. Local Development

For local development, update `iDiski.Api/appsettings.json`:

```json
{
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  }
}
```

To test Cloudinary locally, set the environment variable:
```bash
export USE_CLOUDINARY=true  # Linux/Mac
set USE_CLOUDINARY=true     # Windows CMD
$env:USE_CLOUDINARY="true"  # Windows PowerShell
```

By default, local development uses the `wwwroot/uploads` folder (no Cloudinary needed).

### 5. Deploy and Test

1. Commit and push your changes:
   ```bash
   git add .
   git commit -m "feat: Add Cloudinary file storage for production"
   git push
   ```

2. Railway will automatically redeploy

3. Test by uploading an image through your Angular app (e.g., create/edit a player, team, or sponsor)

4. Verify the uploaded image URL starts with `https://res.cloudinary.com/...`

## How It Works

### File Upload Flow

1. **User uploads image** → Angular app sends file to `/api/uploads`
2. **API receives file** → Routes to `UploadsController`
3. **Cloudinary service** → Uploads file to Cloudinary cloud storage
4. **Returns URL** → `https://res.cloudinary.com/{cloud}/image/upload/{folder}/{uuid}.jpg`
5. **Angular displays image** → Image loads from Cloudinary (not Railway server)

### Storage Configuration

The application automatically chooses the storage backend:
- **Production (Railway)**: Uses Cloudinary (set `USE_CLOUDINARY=true`)
- **Development**: Uses local `wwwroot/uploads` folder

See `iDiski.Api/Program.cs` lines 64-77 for the implementation.

## Cloudinary Features

### Free Tier Limits
- 25 GB storage
- 25 GB bandwidth/month
- 25,000 transformations/month
- More than enough for a sports league site

### Automatic Features
- **Image optimization** - Cloudinary automatically optimizes images
- **CDN delivery** - Fast global delivery via CDN
- **Transformations** - Can resize, crop, format images via URL parameters
- **Responsive images** - Generate different sizes for mobile/desktop

### Example Transformations (Optional)

You can transform images by modifying the URL:

**Original:**
```
https://res.cloudinary.com/demo/image/upload/sample.jpg
```

**Resize to 300x300:**
```
https://res.cloudinary.com/demo/image/upload/w_300,h_300,c_fill/sample.jpg
```

**Thumbnail + auto format:**
```
https://res.cloudinary.com/demo/image/upload/w_150,h_150,c_thumb,f_auto/sample.jpg
```

## Troubleshooting

### Images returning 404

**Symptom**: GET https://izinjuli-production.up.railway.app/uploads/players/abc.jpg 404

**Cause**: Railway is trying to serve files from local filesystem (which doesn't persist)

**Solution**: Ensure `USE_CLOUDINARY=true` is set in Railway environment variables

### "Cloudinary:CloudName not configured" error

**Cause**: Missing Cloudinary credentials in Railway

**Solution**: Add all three environment variables:
- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`

### Images upload but show old Railway URLs

**Cause**: Old images in database still have `/uploads/...` URLs

**Solution**: Re-upload the images after Cloudinary is configured, or run a migration script to update existing URLs

## Database Migration (Optional)

If you have existing images in the database with `/uploads/...` URLs, you'll need to re-upload them to Cloudinary. The old files on Railway are already gone after container restarts.

For new deployments, just ensure Cloudinary is configured before uploading any images.
