# Razorpay Integration Setup Guide

## Issue
You're getting an "Authentication failed" error when trying to create payment orders because the Razorpay API keys in `appsettings.json` are not configured with actual credentials.

## Solution: Get Your Razorpay Keys

### Step 1: Create a Razorpay Account
1. Go to [Razorpay Dashboard](https://dashboard.razorpay.com/)
2. Sign up for a free account
3. Complete the verification process

### Step 2: Get Your API Keys
1. After login, go to **Settings** → **API Keys**
2. You'll see two tabs: **Test** and **Live**
3. For development/testing, use the **Test** keys:
   - **Key ID**: Starts with `rzp_test_...`
   - **Key Secret**: A long string of characters

### Step 3: Update appsettings.json
Replace the placeholder values in your `appsettings.json`:

```json
"Razorpay": {
  "KeyId":     "rzp_test_XXXXXXXXXXXXX",
  "KeySecret": "your_actual_key_secret_here"
}
```

### Step 4: For Production (Live Payments)
When you're ready to go live:
1. Get your **Live** API keys from the same Settings page
2. Update `appsettings.json` with live keys:
   - **Key ID**: Starts with `rzp_live_...`
   - **Key Secret**: Your live key

```json
"Razorpay": {
  "KeyId":     "rzp_live_XXXXXXXXXXXXX",
  "KeySecret": "your_live_key_secret_here"
}
```

## Security Note ⚠️
- **Never commit real API keys to git!**
- Use environment variables or configuration files excluded from git
- For development, use test keys only
- Keep your key secret... secret!

## Common Issues

### "Authentication failed" Error
- ❌ Using placeholder values ("yourkeyid", "yourkeysecret")
- ❌ Invalid or expired API keys
- ❌ Wrong environment keys (mixing test and live)
- ✅ **Solution**: Verify you're using valid test or live keys from Razorpay dashboard

### Test Keys Don't Start with "rzp_test_"
- Your Razorpay account might not have test keys enabled
- Go to Settings → API Keys and make sure both tabs are visible
- If not, verify your account is properly activated

## Testing Payment Flow

### Using Test Cards
Razorpay provides test card numbers for development:

**Successful Payment:**
- Card Number: `4111 1111 1111 1111`
- Expiry: Any future date (e.g., 12/25)
- CVV: Any 3 digits (e.g., 123)

**Failed Payment:**
- Card Number: `4444 3333 2222 1111`

## Support
For more details, visit: https://razorpay.com/docs/api/orders/create/

---

Once you've added your real Razorpay API keys, the payment feature will work correctly!
