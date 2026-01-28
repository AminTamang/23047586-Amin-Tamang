using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Reverie.Services;

public class LockService
{
    private const string PinKey = "reverie.pin";
    private const string PinEnabledKey = "pin_enabled";

    private bool _isUnlocked = false;
    public event EventHandler<bool>? LockStateChanged;
    public int PinLength => 4;

    // Initialize on service creation
    public LockService()
    {
        // Always start locked - don't persist unlocked state across launches
        _isUnlocked = false;
    }

    // Check if PIN is enabled
    public async Task<bool> IsPinEnabledAsync()
    {
        var enabled = await SecureStorage.GetAsync(PinEnabledKey);
        return enabled == "true";
    }

    // Check if PIN exists
    public async Task<bool> HasPinAsync()
    {
        var pin = await SecureStorage.GetAsync(PinKey);
        return !string.IsNullOrWhiteSpace(pin);
    }

    // Set PIN and enable protection
    public async Task SetPinAsync(string pin)
    {
        if (pin.Length != 4 || !pin.All(char.IsDigit))
            throw new Exception("PIN must be 4 digits.");

        await SecureStorage.SetAsync(PinKey, pin);
        await SecureStorage.SetAsync(PinEnabledKey, "true");
        UnlockApp(); // Unlock after setting PIN
    }

    // Clear PIN and disable protection
    public async Task ClearPinAsync()
    {
        SecureStorage.Remove(PinKey);
        await SecureStorage.SetAsync(PinEnabledKey, "false");
        UnlockApp(); // Unlock after clearing PIN
    }

    // Verify PIN
    public async Task<bool> VerifyPinAsync(string pin)
    {
        var saved = await SecureStorage.GetAsync(PinKey);
        var isCorrect = saved == pin;

        if (isCorrect)
        {
            UnlockApp();
        }

        return isCorrect;
    }

    // Enable/disable PIN without setting
    public async Task SetPinEnabledAsync(bool enabled)
    {
        if (enabled)
        {
            await SecureStorage.SetAsync(PinEnabledKey, "true");
        }
        else
        {
            await SecureStorage.SetAsync(PinEnabledKey, "false");
            SecureStorage.Remove(PinKey);
            UnlockApp();
        }
    }

    // Check if app is unlocked
    public bool IsUnlocked()
    {
        return _isUnlocked;
    }

    // Lock the app
    public void LockApp()
    {
        _isUnlocked = false;
        LockStateChanged?.Invoke(this, false);
    }

    // Unlock the app
    private void UnlockApp()
    {
        _isUnlocked = true;
        LockStateChanged?.Invoke(this, true);
    }

    // Check if should show lock screen
    public async Task<bool> ShouldShowLockAsync()
    {
        var hasPin = await HasPinAsync();
        var pinEnabled = await IsPinEnabledAsync();

        // Always show lock if PIN is enabled and app is not currently unlocked
        return hasPin && pinEnabled && !_isUnlocked;
    }
}